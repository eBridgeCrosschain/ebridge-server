using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.BridgeContract;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.ExceptionHandler;
using AElf.ExceptionHandler;
using Volo.Abp.Uow;

namespace AElf.CrossChainServer.Worker;

public abstract class BridgeContractSyncProviderBase : IBridgeContractSyncProvider
{
    public IBridgeContractAppService BridgeContractAppService { get; set; }
    public IBlockchainAppService BlockchainAppService{ get; set; }
    
    private const int MaxRequestCount = 100;

    protected BridgeContractSyncProviderBase()
    {
    }

    public abstract TransferType Type { get; }

    public async Task SyncAsync(string chainId, List<Guid> tokenIds, List<string> targetChainIds)
    {
        var contractSyncIndex =
            await GetReceiveReceiptIndexAsync(chainId, tokenIds, targetChainIds);

        foreach (var index in contractSyncIndex.Where(index => index.Index != 0))
        {
            await HandleReceiptIndexAsync(chainId, index);
        }
    }
    

    [UnitOfWork]
    protected async Task HandleReceiptIndexAsync(string chainId, ReceiptIndexDto index)
    {
        var syncInfo =
            await BridgeContractAppService.GetSyncInfoAsync(chainId, Type, index.TargetChainId, index.TokenId);

        if (syncInfo != null && syncInfo.SyncIndex >= index.Index)
        {
            return;
        }

        var fromIndex = syncInfo == null ? 1 : syncInfo.SyncIndex + 1;
        var endIndex = Math.Min(fromIndex + MaxRequestCount - 1, index.Index);

        var result = await HandleReceiptAsync(chainId, index.TargetChainId, index.TokenId, fromIndex, endIndex);
        if (result.Count == 0)
        {
            return;
        }

        await BridgeContractAppService.UpdateSyncInfoAsync(chainId, Type, index.TargetChainId, index.TokenId,
            fromIndex + result.Count - 1);
    }

    [ExceptionHandler(typeof(Exception),Message = "Get confirmed height failed.",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.ThrowException))]    
    protected async Task<long> GetConfirmedHeightAsync(string chainId)
    {
        var chainStatus = await BlockchainAppService.GetChainStatusAsync(chainId);
        return chainStatus.ConfirmedBlockHeight;
    }

    protected abstract Task<List<ReceiptIndexDto>> GetReceiveReceiptIndexAsync(string chainId, List<Guid> tokenIds,
        List<string> targetChainIds);
    protected abstract Task<HandleReceiptResult> HandleReceiptAsync(string chainId, string targetChainId, Guid tokenId, long fromIndex, long endIndex);
}

public class HandleReceiptResult
{
    public int Count { get; set; }
}