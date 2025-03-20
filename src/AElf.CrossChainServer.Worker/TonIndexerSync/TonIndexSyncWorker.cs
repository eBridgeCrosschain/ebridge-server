using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.Tokens;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Serilog;
using TonSdk.Core;
using TonSdk.Core.Boc;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.CrossChainServer.Worker;

public class TonIndexSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IBlockchainAppService _blockchainAppService;
    private readonly ISettingManager _settingManager;
    private readonly IChainAppService _chainAppService;
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly TonIndexSyncOptions _tonIndexSyncOptions;
    private readonly ICrossChainLimitAppService _crossChainLimitAppService;

    public TonIndexSyncWorker([NotNull] AbpAsyncTimer timer, [NotNull] IServiceScopeFactory serviceScopeFactory,
        IBlockchainAppService blockchainAppService, ISettingManager settingManager,
        IChainAppService chainAppService, ICrossChainTransferAppService crossChainTransferAppService,
        ITokenAppService tokenAppService, IOptionsSnapshot<TonIndexSyncOptions> tonIndexSyncOptions,
        ICrossChainLimitAppService crossChainLimitAppService) : base(
        timer, serviceScopeFactory)
    {
        _blockchainAppService = blockchainAppService;
        _settingManager = settingManager;
        _chainAppService = chainAppService;
        _crossChainTransferAppService = crossChainTransferAppService;
        _tokenAppService = tokenAppService;
        _crossChainLimitAppService = crossChainLimitAppService;
        _tonIndexSyncOptions = tonIndexSyncOptions.Value;
        timer.Period = _tonIndexSyncOptions.SyncPeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        if (!_tonIndexSyncOptions.IsEnable)
        {
            return;
        }
        var chains = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.Tvm
        });

        foreach (var chain in chains.Items)
        {
            if (!_tonIndexSyncOptions.ContractAddress.TryGetValue(chain.Id, out var contract))
            {
                continue;
            }

            Log.Debug("Sync ton chain bridge contract:{contract}", contract.BridgeContract);
            await HandleTonTransactionAsync(chain.Id, contract.BridgeContract);
            foreach (var pool in contract.BridgePoolContract)
            {
                Log.Debug("Sync ton chain bridge pool contract:{contract}", pool.PoolAddress);
                await HandleTonTransactionAsync(chain.Id, pool.PoolAddress, pool.TokenAddress);
            }
        }
    }

    private async Task HandleTonTransactionAsync(string chainId, string contractAddress, string tokenAddress = null)
    {
        var settingKey = GetSettingKey(contractAddress);
        var lastSyncLt =
            await _settingManager.GetOrNullAsync(chainId, settingKey);
        var txs = await _blockchainAppService.GetTonTransactionAsync(new GetTonTransactionInput
        {
            ChainId = chainId,
            ContractAddress = contractAddress,
            LatestTransactionLt = lastSyncLt
        });

        while (true)
        {
            if (!txs.Any() || (txs.Count == 1 && txs[0].Lt == lastSyncLt))
            {
                break;
            }

            foreach (var tx in txs)
            {
                if (tx.Lt == lastSyncLt)
                {
                    continue;
                }

                var txId = tx.Hash;
                var traceId = tx.TraceId;

                foreach (var outMsg in tx.OutMsgs)
                {
                    switch (outMsg.Opcode)
                    {
                        case CrossChainServerConsts.TonReceivedOpCode:
                            await ReceiveAsync(chainId, DateTimeHelper.FromUnixTimeSeconds(tx.Now), outMsg,
                                txId);
                            break;
                        case CrossChainServerConsts.TonTransferredOpCode:
                            await TransferAsync(chainId, tx.McBlockSeqno, DateTimeHelper.FromUnixTimeSeconds(tx.Now),
                                outMsg, traceId, txId);
                            break;
                        case CrossChainServerConsts.TonDailyLimitChangedOpCode:
                            await SetDailyLimitAsync(chainId, tokenAddress, outMsg);
                            break;
                        case CrossChainServerConsts.TonRateLimitChangedOpCode:
                            await SetRateLimitAsync(chainId, tokenAddress, outMsg);
                            break;
                        case CrossChainServerConsts.TonLimitConsumedOpCode:
                            await ConsumeLimitAsync(chainId, tokenAddress, outMsg);
                            break;
                        default:
                            continue;
                    }
                }
            }

            lastSyncLt = txs.Last().Lt;
            await _settingManager.SetAsync(chainId, settingKey, lastSyncLt);

            // Avoid exceeding the request limit
            await Task.Delay(_tonIndexSyncOptions.QueryDelayTime);
            txs = await _blockchainAppService.GetTonTransactionAsync(new GetTonTransactionInput
            {
                ChainId = chainId,
                LatestTransactionLt = lastSyncLt
            });
        }
    }

    private string GetSettingKey(string contractAddress)
    {
        return $"{CrossChainServerSettings.TonIndexTransactionSync}-{contractAddress}";
    }

    private async Task TransferAsync(string chainId, long blockHeight, DateTime blockTime, TonMessageDto outMessage,
        string traceId, string txId)
    {
        Log.ForContext("chainId", chainId).Debug(
            "Sync ton transfer.txId:{txId}", txId);
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var eventId = bodySlice.LoadUInt(32);
        var toChainId = (int)bodySlice.LoadInt(32);
        var amount = bodySlice.LoadCoins().ToBigInt();
        var addInfo = bodySlice.LoadRef().Parse();
        var fromAddress = addInfo.LoadAddress();
        var tokenAddress = addInfo.LoadAddress();
        var toAddress = addInfo.LoadBytes(32);
        var receipt = bodySlice.LoadRef();
        var receiptSlice = receipt.Parse();
        var keyHash = receiptSlice.LoadBytes(32).ToHex();
        var index = receiptSlice.LoadInt(64);
        var receiptId = $"{keyHash}.{index}";

        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress.ToString()
        });

        var toChain = await _chainAppService.GetByAElfChainIdAsync(toChainId);
        var from = TonAddressHelper.GetTonRawAddress(fromAddress.ToString());
        await _crossChainTransferAppService.TransferAsync(new CrossChainTransferInput
        {
            TraceId = traceId,
            TransferTransactionId = txId,
            FromAddress = from,
            ReceiptId = receiptId,
            ToAddress = Types.Address.FromBytes(toAddress).ToBase58(),
            TransferAmount = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals)),
            TransferTime = blockTime,
            FromChainId = chainId,
            ToChainId = toChain.Id,
            TransferBlockHeight = blockHeight,
            TransferTokenId = token.Id
        });
    }

    private async Task ReceiveAsync(string chainId, DateTime blockTime, TonMessageDto outMessage,
        string txId)
    {
        Log.ForContext("chainId", chainId).Debug(
            "Sync ton receive.txId:{txId}", txId);
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var eventId = bodySlice.LoadUInt(32);
        var toAddress = bodySlice.LoadAddress();
        var tokenAddress = bodySlice.LoadAddress();
        var amount = bodySlice.LoadCoins().ToBigInt();
        var fromChainId = (int)bodySlice.LoadInt(32);
        var receipt = bodySlice.LoadRef();
        var receiptSlice = receipt.Parse();
        var keyHash = receiptSlice.LoadBytes(32).ToHex();
        var index = receiptSlice.LoadUInt(256);
        var receiptId = $"{keyHash}.{index}";

        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress.ToString()
        });
        var fromChain = await _chainAppService.GetByAElfChainIdAsync(fromChainId);
        var to = TonAddressHelper.GetTonRawAddress(toAddress.ToString());
        await _crossChainTransferAppService.ReceiveAsync(new CrossChainReceiveInput
        {
            ReceiveTransactionId = txId,
            ReceiptId = receiptId,
            ToAddress = to,
            FromChainId = fromChain.Id,
            ToChainId = chainId,
            ReceiveAmount = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals)),
            ReceiveTime = blockTime,
            ReceiveTokenId = token.Id,
        });
    }

    private async Task SetDailyLimitAsync(string chainId, string tokenAddress, TonMessageDto outMessage)
    {
        Log.ForContext("chainId", chainId).Debug("Start to set daily limit:{tokenAddress}", tokenAddress);
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var eventId = bodySlice.LoadUInt(32);
        var toChainId = (int)bodySlice.LoadInt(32);
        var type = (CrossChainLimitType)(int)bodySlice.LoadUInt(1);
        var remainAmount = bodySlice.LoadInt(256);
        var refreshTime = bodySlice.LoadInt(64);
        var limit = bodySlice.LoadInt(256);

        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress
        });

        await _crossChainLimitAppService.SetCrossChainDailyLimitAsync(new SetCrossChainDailyLimitInput()
        {
            ChainId = chainId,
            Type = type,
            DailyLimit = (decimal)((BigDecimal)limit / BigInteger.Pow(10, token.Decimals)),
            RefreshTime = (long)refreshTime,
            RemainAmount = (decimal)((BigDecimal)remainAmount / BigInteger.Pow(10, token.Decimals)),
            TokenId = token.Id,
            TargetChainId = ChainHelper.ConvertChainIdToBase58(toChainId)
        });
    }

    private async Task ConsumeLimitAsync(string chainId, string tokenAddress, TonMessageDto outMessage)
    {
        Log.ForContext("chainId", chainId).Debug("Start to consume limit:{tokenAddress}", tokenAddress);
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var eventId = bodySlice.LoadUInt(32);
        var toChainId = (int)bodySlice.LoadInt(32);
        var type = (CrossChainLimitType)(int)bodySlice.LoadUInt(1);
        var amount = bodySlice.LoadInt(256);

        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress
        });

        await _crossChainLimitAppService.ConsumeCrossChainDailyLimitAsync(new ConsumeCrossChainDailyLimitInput
        {
            ChainId = chainId,
            Type = type,
            TargetChainId = ChainHelper.ConvertChainIdToBase58(toChainId),
            TokenId = token.Id,
            Amount = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals))
        });
        await _crossChainLimitAppService.ConsumeCrossChainRateLimitAsync(new ConsumeCrossChainRateLimitInput
        {
            ChainId = chainId,
            Type = type,
            TargetChainId = ChainHelper.ConvertChainIdToBase58(toChainId),
            TokenId = token.Id,
            Amount = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals))
        });
    }

    private async Task SetRateLimitAsync(string chainId, string tokenAddress, TonMessageDto outMessage)
    {
        Log.ForContext("chainId", chainId).Debug("Start to set rate limit:{tokenAddress}", tokenAddress);
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var eventId = bodySlice.LoadUInt(32);
        var toChainId = (int)bodySlice.LoadInt(32);
        var type = (CrossChainLimitType)(int)bodySlice.LoadUInt(1);
        var limitInfo = bodySlice.LoadRef().Parse();
        var currentAmount = limitInfo.LoadInt(256);
        var capacity = limitInfo.LoadInt(256);
        var isEnable = limitInfo.LoadInt(1) == 0 ? false : true;
        var rate = limitInfo.LoadInt(256);
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress
        });

        await _crossChainLimitAppService.SetCrossChainRateLimitAsync(new SetCrossChainRateLimitInput()
        {
            ChainId = chainId,
            Type = type,
            TargetChainId = ChainHelper.ConvertChainIdToBase58(toChainId),
            TokenId = token.Id,
            Capacity = (decimal)((BigDecimal)capacity / BigInteger.Pow(10, token.Decimals)),
            IsEnable = isEnable,
            Rate = (decimal)((BigDecimal)rate / BigInteger.Pow(10, token.Decimals)),
            CurrentAmount = (decimal)((BigDecimal)currentAmount / BigInteger.Pow(10, token.Decimals)),
        });
    }

    private async Task ConsumeRateLimitAsync(string chainId, string tokenAddress, TonMessageDto outMessage)
    {
        Log.ForContext("chainId", chainId).Debug("Start to consume rate limit:{tokenAddress}", tokenAddress);
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var eventId = bodySlice.LoadUInt(32);
        var toChainId = (int)bodySlice.LoadInt(32);
        var type = (CrossChainLimitType)(int)bodySlice.LoadUInt(1);
        var amount = bodySlice.LoadInt(256);

        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress
        });

        await _crossChainLimitAppService.ConsumeCrossChainRateLimitAsync(new ConsumeCrossChainRateLimitInput
        {
            ChainId = chainId,
            Type = type,
            TargetChainId = ChainHelper.ConvertChainIdToBase58(toChainId),
            TokenId = token.Id,
            Amount = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals))
        });
    }
}