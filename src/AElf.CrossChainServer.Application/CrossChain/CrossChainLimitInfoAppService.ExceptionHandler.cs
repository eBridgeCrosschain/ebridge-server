using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace AElf.CrossChainServer.CrossChain;

public partial class CrossChainLimitInfoAppService
{
    public async Task<FlowBehavior> HandleGetEvmReceiptRateLimitsException(Exception ex, string chainId, List<string> targetChainIds, List<Guid> tokenIds, List<string> symbols)
    {
        Log.ForContext("fromChainId", chainId).Error(ex,
            "Get evm receipt rate limits failed, FromChainId: {key}, TargetChainId list:{targetChainIds}, Token list:{symbols}",
            chainId, targetChainIds, symbols);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }
    
    public async Task<FlowBehavior> HandleGetEvmSwapRateLimitsException(Exception ex, List<string> fromChainIds, string toChainId, List<Guid> tokenIds, List<string> symbols)
    {
        Log.ForContext("targetChainId", toChainId).Error(ex,
        "Get evm swap rate limits failed,FromChainIds:{fromChainIds}, TargetChainId:{targetChainId}, Token list:{symbol}",
        fromChainIds, toChainId, symbols);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }
    
    public async Task<FlowBehavior> GetEvmRateLimitInfosByChainIdException(Exception ex,string chainId,List<TokenInfo> tokenInfos)
    {
        Log.ForContext("chainId", chainId).Error(ex,
            "Get evm receipt rate limits failed, ChainId: {key}", chainId);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }
}