using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.CrossChain;

public interface ICrossChainLimitInfoAppService
{
    /**
     * to get Daily Limits config
     */
    Task<ListResultDto<CrossChainDailyLimitsDto>> GetCrossChainDailyLimitsAsync();
    /**
     *  to get Rate Limits Infos
     */
    Task<ListResultDto<CrossChainRateLimitsDto>> GetCrossChainRateLimitsAsync();
}