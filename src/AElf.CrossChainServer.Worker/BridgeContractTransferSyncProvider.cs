using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.BridgeContract;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.ExceptionHandler;
using AElf.ExceptionHandler;
using Serilog;
using Volo.Abp.Domain.Entities;

namespace AElf.CrossChainServer.Worker;

public class BridgeContractTransferSyncProvider :BridgeContractSyncProviderBase
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;

    public BridgeContractTransferSyncProvider(
        ICrossChainTransferAppService crossChainTransferAppService) 
    {
        _crossChainTransferAppService = crossChainTransferAppService;
    }

    public override TransferType Type { get; } = TransferType.Transfer;
    
    protected override async Task<List<ReceiptIndexDto>> GetReceiveReceiptIndexAsync(string chainId, List<Guid> tokenIds, List<string> targetChainIds)
    {
        return await BridgeContractAppService.GetTransferReceiptIndexAsync(chainId, tokenIds, targetChainIds);
    }
    
    [ExceptionHandler(typeof(Exception), typeof(EntityNotFoundException),
        TargetType = typeof(BridgeContractTransferSyncProvider),
        MethodName = nameof(HandleReceiptException))]
    protected override async Task<HandleReceiptResult> HandleReceiptAsync(string chainId, string targetChainId, Guid tokenId, long fromIndex, long endIndex)
    {
        var result = new HandleReceiptResult();

        var receipts = await BridgeContractAppService.GetTransferReceiptInfosAsync(chainId, targetChainId, tokenId,
            fromIndex, endIndex);
        if (receipts.Count != 0)
        {
            var lib = await GetConfirmedHeightAsync(chainId);
            var count = 0;
            foreach (var receipt in receipts)
            {
                if (receipt.BlockHeight >= lib)
                {
                    break;
                }

                await _crossChainTransferAppService.TransferAsync(new CrossChainTransferInput
                {
                    FromAddress = receipt.FromAddress,
                    ReceiptId = receipt.ReceiptId,
                    ToAddress = receipt.ToAddress,
                    TransferAmount = receipt.Amount,
                    TransferTime = receipt.BlockTime,
                    FromChainId = chainId,
                    ToChainId = receipt.ToChainId,
                    TransferBlockHeight = receipt.BlockHeight,
                    TransferTokenId = receipt.TokenId
                });
                count++;
            }

            result.Count = count;
        }

        return result;
    }
    private async Task<FlowBehavior> HandleReceiptException(Exception ex, string chainId, string targetChainId, Guid tokenId, long fromIndex, long endIndex)
    {
        Log.ForContext("chainId", chainId).Error(ex,
            "Handle receipt failed, ChainId: {key}, TargetChainId: {targetChainId}, TokenId: {tokenId}, FromIndex: {fromIndex}, EndIndex: {endIndex}", chainId, targetChainId, tokenId, fromIndex, endIndex);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new HandleReceiptResult()
        };
    }
}