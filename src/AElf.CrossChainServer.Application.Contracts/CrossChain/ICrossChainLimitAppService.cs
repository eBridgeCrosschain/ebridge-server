using System.Threading.Tasks;

namespace AElf.CrossChainServer.CrossChain;

public interface ICrossChainLimitAppService
{
    Task SetCrossChainRateLimitAsync(SetCrossChainRateLimitInput input);
    Task ConsumeCrossChainRateLimitAsync(ConsumeCrossChainRateLimitInput input);
    Task SetCrossChainRateLimitIndexAsync(SetCrossChainRateLimitInput input);
    Task SetCrossChainDailyLimitAsync(SetCrossChainDailyLimitInput input);
    Task ConsumeCrossChainDailyLimitAsync(ConsumeCrossChainDailyLimitInput input);
    Task SetCrossChainDailyLimitIndexAsync(SetCrossChainDailyLimitInput input);
}