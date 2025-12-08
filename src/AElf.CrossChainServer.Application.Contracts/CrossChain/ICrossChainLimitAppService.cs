using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.CrossChainServer.CrossChain;

public interface ICrossChainLimitAppService
{
    Task InitLimitAsync();
    Task SetCrossChainRateLimitAsync(SetCrossChainRateLimitInput input);
    Task ConsumeCrossChainRateLimitAsync(ConsumeCrossChainRateLimitInput input);
    Task SetCrossChainRateLimitIndexAsync(SetCrossChainRateLimitInput input);
    Task<List<CrossChainRateLimitDto>> GetCrossChainRateLimitsAsync();
    Task SetCrossChainDailyLimitAsync(SetCrossChainDailyLimitInput input);
    Task ConsumeCrossChainDailyLimitAsync(ConsumeCrossChainDailyLimitInput input);
    Task SetCrossChainDailyLimitIndexAsync(SetCrossChainDailyLimitInput input);
}