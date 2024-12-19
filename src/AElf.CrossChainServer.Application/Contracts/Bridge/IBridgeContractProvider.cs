using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.TokenPool;

namespace AElf.CrossChainServer.Contracts.Bridge;

public interface IBridgeContractProvider
{
    BlockchainType ChainType { get; }

    Task<List<ReceiptInfoDto>> GetSendReceiptInfosAsync(string chainId, string contractAddress, string targetChainId, Guid tokenId,
        long fromIndex, long endIndex);

    Task<List<ReceivedReceiptInfoDto>> GetReceivedReceiptInfosAsync(string chainId, string contractAddress, string fromChainId, Guid tokenId,
        long fromIndex, long endIndex);
    
    Task<List<ReceiptIndexDto>> GetTransferReceiptIndexAsync(string chainId, string contractAddress, List<Guid> tokenIds, List<string> targetChainIds);
    
    Task<List<ReceiptIndexDto>> GetReceiveReceiptIndexAsync(string chainId, string contractAddress, List<Guid> tokenIds, List<string> fromChainIds);

    Task<bool> CheckTransmitAsync(string chainId, string contractAddress, string receiptHash);
    
    Task<string> GetSwapIdByTokenAsync(string chainId, string contractAddress, string fromChainId, string symbol);
    Task<string> SwapTokenAsync(string chainId, string contractAddress, string privateKey, string swapId, string receiptId, string originAmount,
        string receiverAddress);

    Task<List<TokenBucketDto>> GetCurrentReceiptTokenBucketStatesAsync(string chainId, string contractAddress, List<Guid> tokenIds, List<string> targetChainIds);
    Task<List<TokenBucketDto>> GetCurrentSwapTokenBucketStatesAsync(string chainId, string contractAddress, List<Guid> tokenIds, List<string> fromChainIds);
    
    Task<List<PoolLiquidityDto>> GetPoolLiquidityAsync(string chainId, string contractAddress,List<Guid> tokenIds);
}