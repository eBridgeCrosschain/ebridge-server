using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.Tokens;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using Log = Serilog.Log;

namespace AElf.CrossChainServer.Worker;

public class SolanaIndexSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IBlockchainAppService _blockchainAppService;
    private readonly ISettingManager _settingManager;
    private readonly IChainAppService _chainAppService;
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly SolanaIndexSyncOptions _solanaIndexSyncOptions;
    private readonly ICrossChainLimitAppService _crossChainLimitAppService;
    private const string Instruction = "Instruction: ";
    private const string ProgramData = "Program data: ";
    private const int limit = 100;

    public SolanaIndexSyncWorker([NotNull] AbpAsyncTimer timer, [NotNull] IServiceScopeFactory serviceScopeFactory,
        IBlockchainAppService blockchainAppService, ISettingManager settingManager,
        IChainAppService chainAppService, ICrossChainTransferAppService crossChainTransferAppService,
        ITokenAppService tokenAppService, IOptionsSnapshot<SolanaIndexSyncOptions> solanaIndexSyncOptions,
        ICrossChainLimitAppService crossChainLimitAppService) : base(
        timer, serviceScopeFactory)
    {
        _blockchainAppService = blockchainAppService;
        _settingManager = settingManager;
        _chainAppService = chainAppService;
        _crossChainTransferAppService = crossChainTransferAppService;
        _tokenAppService = tokenAppService;
        _crossChainLimitAppService = crossChainLimitAppService;
        _solanaIndexSyncOptions = solanaIndexSyncOptions.Value;
        timer.Period = _solanaIndexSyncOptions.SyncPeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        if (!_solanaIndexSyncOptions.IsEnable)
        {
            return;
        }
        var chains = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.Svm
        });

        foreach (var chain in chains.Items)
        {
            if (!_solanaIndexSyncOptions.ContractAddress.TryGetValue(chain.Id, out var contract))
            {
                continue;
            }

            Log.Debug("Sync solana chain token pool contract:{contract}", contract.TokenPoolContract);
            await HandleSolanaTransactionAsync(chain.Id, contract.TokenPoolContract);
            Log.Debug("Sync solana chain bridge contract:{contract}", contract.BridgeContract);
            await HandleSolanaTransactionAsync(chain.Id, contract.BridgeContract);
        }
    }

    private async Task HandleSolanaTransactionAsync(string chainId, string contractAddress)
    {
        var settingKey = GetSettingKey(contractAddress);
        var lastSig =
            await _settingManager.GetOrNullAsync(chainId, settingKey);
        lastSig = lastSig.IsNullOrEmpty() ? null : lastSig;
        var signatures = await _blockchainAppService.GetSignaturesForAddressAsync(chainId, 
            contractAddress,
            limit,
            null,
            lastSig);
        var latest = signatures?.FirstOrDefault();

        while (true)
        {
            if (signatures == null)
            {
                Log.ForContext("chainId", chainId).Debug("Sync solana last sig reset null.");
                await _settingManager.SetAsync(chainId, settingKey, null);
                break;
            } 
            if(!signatures.Any())
            {
                break;
            }

            signatures.Reverse();
            foreach (var sig in signatures)
            {
                var tx = await _blockchainAppService.GetSolanaTransactionAsync(chainId, sig);
                if (tx.Meta.Error != null || !tx.Meta.LogMessages.Any()) continue;
                if (CheckInstruction(contractAddress, CrossChainServerConsts.SolanaLockInstruction, tx))
                {
                    await LockAsync(chainId, sig, tx);
                }
                if (CheckInstruction(contractAddress, CrossChainServerConsts.SolanaReleaseInstruction, tx))
                {
                    await ReleaseAsync(chainId, sig, tx);
                }
                if (CheckInstruction(contractAddress, CrossChainServerConsts.SolanaDailyLimitChangedInstruction, tx))
                {
                    await SetDailyLimitAsync(chainId, tx);
                }
                if (CheckInstruction(contractAddress, CrossChainServerConsts.SolanaRateLimitChangedInstruction, tx))
                {
                    await SetRateLimitAsync(chainId, tx);
                }
                if (CheckInstruction(contractAddress, CrossChainServerConsts.SolanaLimitConsumedInstruction, tx))
                {
                    await ConsumeLimitAsync(chainId, tx);
                }
            }

            await Task.Delay(_solanaIndexSyncOptions.QueryDelayTime);
            if (signatures.Count < limit) break;
            signatures = await _blockchainAppService.GetSignaturesForAddressAsync(chainId, 
                contractAddress,
                limit,
                signatures.First(),
                lastSig);
        }

        if (latest != null)
        {
            await _settingManager.SetAsync(chainId, settingKey, latest);
        }
    }

    private string GetSettingKey(string contractAddress)
    {
        return $"{CrossChainServerSettings.SolanaIndexTransactionSync}-{contractAddress}";
    }

    private bool CheckInstruction(string contractAddress, string key, TransactionMetaSlotInfo tx)
    {
        return tx.Meta.LogMessages.First().Contains(contractAddress) 
               && tx.Meta.LogMessages.Any(t => t.Contains($"{Instruction}{key}"));
    }

    private string GetProgramDataValue(List<string> logs, string key)
    {
        var found = false;
        var seq = 0;
        foreach (var str in logs)
        {
            if (found && str.StartsWith(ProgramData))
            {
                seq += 1;
                if ((key == CrossChainServerConsts.SolanaLockInstruction || 
                    key == CrossChainServerConsts.SolanaReleaseInstruction) && seq == 1)
                {
                    continue;
                }
                return str.Substring(ProgramData.Length).Trim();
            }

            if (str.Contains($"{Instruction}{key}"))
            {
                found = true;
            }
        }
        return null;
    }
    
    private string ReadString(byte[] data, ref int offset)
    {
        var length = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset));
        offset += 4;
        var value = Encoding.UTF8.GetString(data, offset, length);
        offset += length;
        return value;
    }

    private PublicKey ReadPublicKey(byte[] data, ref int offset)
    {
        var pubkeyBytes = new byte[32];
        Buffer.BlockCopy(data, offset, pubkeyBytes, 0, 32);
        return new PublicKey(pubkeyBytes);
    }
    
    private int ReadByte(byte[] data, ref int offset)
    {
        var bytes = new byte[1];
        Buffer.BlockCopy(data, offset, bytes, 0, 1);
        return (int)bytes[0];
    }

    private async Task LockAsync(string chainId, string signature, TransactionMetaSlotInfo tx)
    {
        Log.ForContext("chainId", chainId).Debug("Sync solana lock.txId:{txId}", signature);
        var eventData = GetProgramDataValue(tx.Meta.LogMessages.ToList(), CrossChainServerConsts.SolanaLockInstruction);
        if (eventData.IsNullOrEmpty()) return;
        
        var data = Convert.FromBase64String(eventData);
        var offset = 8;
        var receiptId = ReadString(data, ref offset);
        var amount = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var owner = ReadPublicKey(data, ref offset);
        offset += 32;
        var symbol = ReadString(data, ref offset);
        var targetAddress = ReadString(data, ref offset);
        var targetChainId = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));
        offset += 2;
        var tokenAddress = ReadPublicKey(data, ref offset);
        
        var block = await _blockchainAppService.GetSolanaBlockAsync(chainId, tx.Slot);
        var token = await _blockchainAppService.GetTokenInfoAsync(chainId, tokenAddress?.ToString());
        var toChain = await _chainAppService.GetByAElfChainIdAsync(targetChainId);
        await _crossChainTransferAppService.TransferAsync(new CrossChainTransferInput
        {
            TraceId = block?.Blockhash,
            TransferTransactionId = signature,
            FromAddress = owner?.ToString(),
            ReceiptId = receiptId,
            ToAddress = targetAddress,
            TransferAmount = token == null || token.Decimals == 0 ? 0M : (decimal)(amount / BigInteger.Pow(10, token.Decimals)),
            TransferTime = block == null || block.BlockTime == 0 ? DateTime.MinValue : DateTimeHelper.FromUnixTimeSeconds(block.BlockTime),
            FromChainId = chainId,
            ToChainId = toChain?.Id ?? "0",
            TransferBlockHeight = block == null || block.BlockHeight == null ? 0L : block.BlockHeight.Value,
            TransferTokenId = token?.Id ?? Guid.Empty,
        });
    }
    
    private async Task ReleaseAsync(string chainId, string signature, TransactionMetaSlotInfo tx)
    {
        Log.ForContext("chainId", chainId).Debug("Sync solana release.txId:{txId}", signature);
        var eventData = GetProgramDataValue(tx.Meta.LogMessages.ToList(), CrossChainServerConsts.SolanaReleaseInstruction);
        if (eventData.IsNullOrEmpty()) return;
        
        var data = Convert.FromBase64String(eventData);
        var offset = 8;
        var amount = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var toAddress = ReadPublicKey(data, ref offset);
        offset += 32;
        var symbol = ReadString(data, ref offset);
        var receiptId = ReadString(data, ref offset);
        var fromChainId = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));
        offset += 2;
        var tokenAddress = ReadPublicKey(data, ref offset);
    
        var block = await _blockchainAppService.GetSolanaBlockAsync(chainId, tx.Slot);
        var token = await _blockchainAppService.GetTokenInfoAsync(chainId, tokenAddress?.ToString());
        var fromChain = await _chainAppService.GetByAElfChainIdAsync(fromChainId);
        await _crossChainTransferAppService.ReceiveAsync(new CrossChainReceiveInput
        {
            ReceiveTransactionId = signature,
            ReceiptId = receiptId,
            ToAddress = toAddress?.ToString(),
            FromChainId = fromChain?.Id ?? "0",
            ToChainId = chainId,
            ReceiveAmount = token == null || token.Decimals == 0 ? 0M : (decimal)(amount / BigInteger.Pow(10, token.Decimals)),
            ReceiveTime = block == null || block.BlockTime == 0 ? DateTime.MinValue : DateTimeHelper.FromUnixTimeSeconds(block.BlockTime),
            ReceiveTokenId = token?.Id ?? Guid.Empty,
        });
    }
    
    private async Task SetDailyLimitAsync(string chainId, TransactionMetaSlotInfo tx)
    {
        Log.ForContext("chainId", chainId).Debug("Start to set solana daily limit.");
        var eventData = GetProgramDataValue(tx.Meta.LogMessages.ToList(), CrossChainServerConsts.SolanaDailyLimitChangedInstruction);
        if (eventData.IsNullOrEmpty()) return;
        
        var data = Convert.FromBase64String(eventData);
        var offset = 8;
        var targetChainId = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));
        offset += 2;
        var tokenAddress = ReadPublicKey(data, ref offset);
        offset += 32;
        var limitType = ReadByte(data, ref offset);
        offset += 1;
        var remainAmount = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var refreshTime = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var dailyLimit = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
    
        var token = await _blockchainAppService.GetTokenInfoAsync(chainId, tokenAddress?.ToString());
        await _crossChainLimitAppService.SetCrossChainDailyLimitAsync(new SetCrossChainDailyLimitInput
        {
            ChainId = chainId,
            Type = (CrossChainLimitType)limitType,
            DailyLimit = token == null || token.Decimals == 0 ? 0M : (decimal)(dailyLimit / BigInteger.Pow(10, token.Decimals)),
            RefreshTime = (long)refreshTime,
            RemainAmount = token == null || token.Decimals == 0 ? 0M : (decimal)(remainAmount / BigInteger.Pow(10, token.Decimals)),
            TokenId = token?.Id ?? Guid.Empty,
            TargetChainId = ChainHelper.ConvertChainIdToBase58(targetChainId)
        });
    }
    
    private async Task SetRateLimitAsync(string chainId, TransactionMetaSlotInfo tx)
    {
        Log.ForContext("chainId", chainId).Debug("Start to set solana rate limit.");
        var eventData = GetProgramDataValue(tx.Meta.LogMessages.ToList(), CrossChainServerConsts.SolanaRateLimitChangedInstruction);
        if (eventData.IsNullOrEmpty()) return;
        
        var data = Convert.FromBase64String(eventData);
        var offset = 8;
        var targetChainId = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));
        offset += 2;
        var tokenAddress = ReadPublicKey(data, ref offset);
        offset += 32;
        var limitType = ReadByte(data, ref offset);
        offset += 1;
        var isEnable = ReadByte(data, ref offset) == 1;
        offset += 1;
        var lastUpdatedTime = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var currentAmount = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var tokenCapacity = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var rate = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
    
        var token = await _blockchainAppService.GetTokenInfoAsync(chainId, tokenAddress?.ToString());
        await _crossChainLimitAppService.SetCrossChainRateLimitAsync(new SetCrossChainRateLimitInput()
        {
            ChainId = chainId,
            Type = (CrossChainLimitType)limitType,
            TargetChainId = ChainHelper.ConvertChainIdToBase58(targetChainId),
            TokenId = token?.Id ?? Guid.Empty,
            Capacity = token == null || token.Decimals == 0 ? 0M : (decimal)(tokenCapacity / BigInteger.Pow(10, token.Decimals)),
            IsEnable = isEnable,
            Rate = token == null || token.Decimals == 0 ? 0M : (decimal)(rate / BigInteger.Pow(10, token.Decimals)),
            CurrentAmount = token == null || token.Decimals == 0 ? 0M : (decimal)(currentAmount / BigInteger.Pow(10, token.Decimals))
        });
    }
    
    private async Task ConsumeLimitAsync(string chainId, TransactionMetaSlotInfo tx)
    {
        Log.ForContext("chainId", chainId).Debug("Start to solana consume limit.");
        var eventData = GetProgramDataValue(tx.Meta.LogMessages.ToList(), CrossChainServerConsts.SolanaLimitConsumedInstruction);
        if (eventData.IsNullOrEmpty()) return;
        
        var data = Convert.FromBase64String(eventData);
        var offset = 8;
        var targetChainId = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));
        offset += 2;
        var tokenAddress = ReadPublicKey(data, ref offset);
        offset += 32;
        var limitType = ReadByte(data, ref offset);
        offset += 1;
        var remainAmount = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var refreshTime = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var dailyLimit = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var isEnable = ReadByte(data, ref offset) == 1;
        offset += 1;
        var lastUpdatedTime = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var currentAmount = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var tokenCapacity = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
        offset += 8;
        var rate = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
    
        var token = await _blockchainAppService.GetTokenInfoAsync(chainId, tokenAddress?.ToString());
        await _crossChainLimitAppService.ConsumeCrossChainDailyLimitAsync(new ConsumeCrossChainDailyLimitInput
        {
            blockchainType = BlockchainType.Svm,
            ChainId = chainId,
            Type = (CrossChainLimitType)limitType,
            TargetChainId = ChainHelper.ConvertChainIdToBase58(targetChainId),
            TokenId = token?.Id ?? Guid.Empty,
            Amount = token == null || token.Decimals == 0 ? 0M : (decimal)(remainAmount / BigInteger.Pow(10, token.Decimals))
        });
        await _crossChainLimitAppService.ConsumeCrossChainRateLimitAsync(new ConsumeCrossChainRateLimitInput
        {
            blockchainType = BlockchainType.Svm,
            ChainId = chainId,
            Type = (CrossChainLimitType)limitType,
            TargetChainId = ChainHelper.ConvertChainIdToBase58(targetChainId),
            TokenId = token?.Id ?? Guid.Empty,
            Amount = token == null || token.Decimals == 0 ? 0M : (decimal)(currentAmount / BigInteger.Pow(10, token.Decimals))
        });
    }
}