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
        var chains = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.Tvm
        });

        foreach (var chain in chains.Items)
        {
            if (!_tonIndexSyncOptions.ContractAddress.TryGetValue(chain.Id, out var contractAddresses))
            {
                continue;
            }

            foreach (var contractAddress in contractAddresses)
            {
                await HandleTonTransactionAsync(chain.Id, contractAddress);
            }
        }
    }

    private async Task HandleTonTransactionAsync(string chainId, string contractAddress)
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
                        case CrossChainServerConsts.TonTransferedOpCode:
                            await TransferAsync(chainId, tx.McBlockSeqno, DateTimeHelper.FromUnixTimeSeconds(tx.Now),
                                outMsg, traceId, txId);
                            break;
                        case CrossChainServerConsts.TonReceivedOpCode:
                            await ReceiveAsync(chainId, DateTimeHelper.FromUnixTimeSeconds(tx.Now), outMsg, traceId,
                                txId);
                            break;
                        case CrossChainServerConsts.TonDailyLimitChangedOpCode:
                            await SetDailyLimitAsync(chainId, tx.Account, outMsg);
                            break;
                        case CrossChainServerConsts.TonDailyLimitConsumedOpCode:
                            await ConsumeDailyLimitAsync(chainId, tx.Account, outMsg);
                            break;
                        case CrossChainServerConsts.TonRateLimitChangedOpCode:
                            await SetRateLimitAsync(chainId, tx.Account, outMsg);
                            break;
                        case CrossChainServerConsts.TonRateLimitConsumedOpCode:
                            await ConsumeRateLimitAsync(chainId, tx.Account, outMsg);
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
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var fromAddress = bodySlice.LoadAddress();
        var toChainId = (int)bodySlice.LoadInt(32);
        var toAddress = bodySlice.LoadAddress();
        var tokenAddress = bodySlice.LoadAddress();
        var amount = bodySlice.LoadInt(256);
        var receipt = bodySlice.LoadRef();
        var receiptSlice = receipt.Parse();
        var keyHash = receiptSlice.LoadBytes(32).ToHex();
        var index = receiptSlice.LoadInt(256);
        var receiptId = $"{keyHash}-{index}";

        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress.ToString()
        });

        await _crossChainTransferAppService.TransferAsync(new CrossChainTransferInput
        {
            TraceId = traceId,
            TransferTransactionId = txId,
            FromAddress = fromAddress.ToString(),
            ReceiptId = receiptId,
            ToAddress = toAddress.ToString(),
            TransferAmount = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals)),
            TransferTime = blockTime,
            FromChainId = chainId,
            ToChainId = ChainHelper.ConvertChainIdToBase58(toChainId),
            TransferBlockHeight = blockHeight,
            TransferTokenId = token.Id
        });
    }

    private async Task ReceiveAsync(string chainId, DateTime blockTime, TonMessageDto outMessage, string traceId,
        string txId)
    {
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var toChainId = (int)bodySlice.LoadInt(32);
        var toAddress = bodySlice.LoadAddress();
        var tokenAddress = bodySlice.LoadAddress();
        var amount = bodySlice.LoadInt(256);
        var receipt = bodySlice.LoadRef();
        var receiptSlice = receipt.Parse();
        var keyHash = receiptSlice.LoadBytes(32).ToHex();
        var index = receiptSlice.LoadInt(256);
        var receiptId = $"{keyHash}-{index}";

        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress.ToString()
        });

        await _crossChainTransferAppService.ReceiveAsync(new CrossChainReceiveInput
        {
            TransferTransactionId = txId,
            TraceId = traceId,
            ReceiptId = receiptId,
            ToAddress = toAddress.ToString(),
            FromChainId = ChainHelper.ConvertChainIdToBase58(toChainId),
            ToChainId = chainId,
            ReceiveAmount = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals)),
            ReceiveTime = blockTime,
            ReceiveTokenId = token.Id
        });
    }

    private async Task SetDailyLimitAsync(string chainId, string account, TonMessageDto outMessage)
    {
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var toChainId = (int)bodySlice.LoadInt(32);
        var type = (CrossChainLimitType)(int)bodySlice.LoadInt(1);
        var remainAmount = bodySlice.LoadInt(256);
        var refreshTime = bodySlice.LoadInt(64);
        var limit = bodySlice.LoadInt(256);

        var tokenAddress = new Address(account);
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress.ToString()
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

    private async Task ConsumeDailyLimitAsync(string chainId, string account, TonMessageDto outMessage)
    {
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var toChainId = (int)bodySlice.LoadInt(32);
        var type = (CrossChainLimitType)(int)bodySlice.LoadInt(1);
        var amount = bodySlice.LoadInt(256);

        var tokenAddress = new Address(account);
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress.ToString()
        });

        await _crossChainLimitAppService.ConsumeCrossChainDailyLimitAsync(new ConsumeCrossChainDailyLimitInput
        {
            ChainId = chainId,
            Type = type,
            TargetChainId = ChainHelper.ConvertChainIdToBase58(toChainId),
            TokenId = token.Id,
            Amount = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals))
        });
    }

    private async Task SetRateLimitAsync(string chainId, string account, TonMessageDto outMessage)
    {
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var toChainId = (int)bodySlice.LoadInt(32);
        var type = (CrossChainLimitType)(int)bodySlice.LoadInt(1);
        var currentAmount = bodySlice.LoadInt(256);
        var capacity = bodySlice.LoadInt(256);
        var isEnable = Convert.ToBoolean(bodySlice.LoadInt(1));
        var rate = bodySlice.LoadInt(256);

        var tokenAddress = new Address(account);
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress.ToString()
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

    private async Task ConsumeRateLimitAsync(string chainId, string account, TonMessageDto outMessage)
    {
        var body = Cell.From(outMessage.MessageContent.Body);
        var bodySlice = body.Parse();
        var toChainId = (int)bodySlice.LoadInt(32);
        var type = (CrossChainLimitType)(int)bodySlice.LoadInt(1);
        var amount = bodySlice.LoadInt(256);

        var tokenAddress = new Address(account);
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress.ToString()
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