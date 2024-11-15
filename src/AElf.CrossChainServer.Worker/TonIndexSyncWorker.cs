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

    public TonIndexSyncWorker([NotNull] AbpAsyncTimer timer, [NotNull] IServiceScopeFactory serviceScopeFactory,
        IBlockchainAppService blockchainAppService, ISettingManager settingManager,
        IChainAppService chainAppService, ICrossChainTransferAppService crossChainTransferAppService,
        ITokenAppService tokenAppService, IOptionsSnapshot<TonIndexSyncOptions> tonIndexSyncOptions) : base(
        timer, serviceScopeFactory)
    {
        _blockchainAppService = blockchainAppService;
        _settingManager = settingManager;
        _chainAppService = chainAppService;
        _crossChainTransferAppService = crossChainTransferAppService;
        _tokenAppService = tokenAppService;
        _tonIndexSyncOptions = tonIndexSyncOptions.Value;
        timer.Period = 10 * 1000;
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
                
                foreach (var outMsg in tx.OutMsgs)
                {
                    switch (outMsg.Opcode)
                    {
                        case CrossChainServerConsts.TonTransferOpCode:
                            await TransferAsync(chainId, tx.McBlockSeqno, DateTimeHelper.FromUnixTimeSeconds(tx.Now),
                                outMsg);
                            break;
                        case CrossChainServerConsts.TonReceiveOpCode:
                            await ReceiveAsync(chainId, DateTimeHelper.FromUnixTimeSeconds(tx.Now), outMsg);
                            break;
                        case CrossChainServerConsts.TonSetPoolLimitOpCode:
                            
                            break;
                        default:
                            continue;
                    }
                }
            }

            lastSyncLt = txs.Last().Lt;
            await _settingManager.SetAsync(chainId, settingKey, lastSyncLt);

            await Task.Delay(1000);
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

    private async Task TransferAsync(string chainId, long blockHeight, DateTime blockTime, TonMessageDto outMessage)
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
            FromAddress = fromAddress.ToString(),
            ReceiptId = receiptId,
            ToAddress = toAddress.ToString(),
            TransferAmount =  (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals)),
            TransferTime = blockTime,
            FromChainId = chainId,
            ToChainId = ChainHelper.ConvertChainIdToBase58(toChainId),
            TransferBlockHeight = blockHeight,
            TransferTokenId = token.Id
        });
    }
    
    private async Task ReceiveAsync(string chainId, DateTime blockTime, TonMessageDto outMessage)
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
            ReceiptId = receiptId,
            ToAddress = toAddress.ToString(),
            FromChainId = ChainHelper.ConvertChainIdToBase58(toChainId),
            ToChainId = chainId,
            ReceiveAmount = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals)),
            ReceiveTime = blockTime,
            ReceiveTokenId = token.Id
        });
    }
}