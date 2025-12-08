using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.TokenPool;

namespace AElf.CrossChainServer.Contracts.Bridge;

public interface IBridgeContractProvider
{
    BlockchainType ChainType { get; }

    Task<DailyLimitDto> GetReceiptDailyLimitAsync(string chainId, string contractAddress, Guid tokenId, string targetChainId);
    Task<DailyLimitDto> GetSwapDailyLimitAsync(string chainId, string contractAddress, string swapId);
    Task<List<TokenBucketDto>> GetCurrentReceiptTokenBucketStatesAsync(string chainId, string contractAddress, List<Guid> tokenIds, List<string> targetChainIds);
    Task<List<TokenBucketDto>> GetCurrentSwapTokenBucketStatesAsync(string chainId, string contractAddress, List<Guid> tokenIds, List<string> fromChainIds);
    
    Task<List<PoolLiquidityDto>> GetPoolLiquidityAsync(string chainId, string contractAddress,List<Guid> tokenIds);
}