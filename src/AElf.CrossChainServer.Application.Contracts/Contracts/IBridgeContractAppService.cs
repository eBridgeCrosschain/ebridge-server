using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.BridgeContract;
using AElf.CrossChainServer.TokenPool;

namespace AElf.CrossChainServer.Contracts;

public interface IBridgeContractAppService
{
    Task<DailyLimitDto> GetDailyLimitAsync(string chainId, Guid tokenId, string targetChainId);
    Task<DailyLimitDto> GetSwapDailyLimitAsync(string chainId, string swapId);

    Task<List<TokenBucketDto>> GetCurrentReceiptTokenBucketStatesAsync(string chainId, List<Guid> tokenIds, List<string> targetChainIds);
    Task<List<TokenBucketDto>> GetCurrentSwapTokenBucketStatesAsync(string chainId, List<Guid> tokenIds, List<string> fromChainIds);
    
    Task<List<PoolLiquidityDto>> GetPoolLiquidityAsync(string chainId, string contractAddress,List<Guid> tokenIds);

    
}