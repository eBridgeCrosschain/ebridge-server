using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace AElf.CrossChainServer.Contracts.Bridge;

public partial class EvmBridgeContractProvider
{
    private async Task<FlowBehavior> HandleGetReceivedReceiptInfosException(Exception ex, string chainId,
        string contractAddress,
        string fromChainId, Guid tokenId,
        long fromIndex, long endIndex)
    {
        Log.ForContext("chainId", chainId).Error(ex,
            "Get received receipt infos failed, ChainId: {key}, ContractAddress: {contractAddress}, FromChainId: {fromChainId}, TokenId: {tokenId}, FromIndex: {fromIndex}, EndIndex: {endIndex}",
            chainId, contractAddress, fromChainId, tokenId, fromIndex, endIndex);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new List<ReceivedReceiptInfoDto>()
        };
    }

    private async Task<FlowBehavior> HandleGetTransferReceiptInfosException(Exception ex, string chainId,
        string contractAddress,
        List<Guid> tokenIds, List<string> targetChainIds)
    {
        Log.ForContext("chainId", chainId).Error(ex,
            "Get transfer receipt infos failed, ChainId: {key}, ContractAddress: {contractAddress}, TokenIds: {tokenIds}, TargetChainIds: {targetChainIds}",
            chainId, contractAddress, tokenIds, targetChainIds);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new List<ReceiptIndexDto>()
        };
    }


    private async Task<FlowBehavior> HandleGetCurrentReceiptTokenBucketStatesException(Exception ex, string chainId,
        string contractAddress, List<Guid> tokenIds,
        List<string> targetChainIds)
    {
        Log.ForContext("chainId", chainId).Error(ex,
            "Get current receipt token bucket states failed, ChainId: {key}, ContractAddress: {contractAddress}, TokenIds: {tokenIds}, TargetChainIds: {targetChainIds}",
            chainId, contractAddress, tokenIds, targetChainIds);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new List<TokenBucketDto>()
        };
    }

    private async Task<FlowBehavior> HandleGetCurrentSwapTokenBucketStatesException(Exception ex, string chainId,
        string contractAddress,
        List<Guid> tokenIds, List<string> fromChainIds)
    {
        Log.ForContext("chainId", chainId).Error(ex,
            "Get current swap token bucket states failed, ChainId: {key}, ContractAddress: {contractAddress}, TokenIds: {tokenIds}, FromChainIds: {fromChainIds}",
            chainId, contractAddress, tokenIds, fromChainIds);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new List<TokenBucketDto>()
        };
    }
}