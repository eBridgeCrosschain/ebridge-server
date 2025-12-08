using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer;

[RemoteService]
[Area("app")]
[ControllerName("CrossChainLimit")]
[Route("api/app/limiter")]
public class CrossChainLimitController
{
    private readonly ICrossChainLimitInfoAppService _crossChainLimitInfoAppService;

    public CrossChainLimitController(ICrossChainLimitInfoAppService crossChainLimitInfoAppService)
    {
        _crossChainLimitInfoAppService = crossChainLimitInfoAppService;
    }

    [HttpGet]
    [Route("dailyLimits")]
    public Task<ListResultDto<CrossChainDailyLimitsDto>> GetCrossChainDailyLimitsAsync()
    {
        return _crossChainLimitInfoAppService.GetCrossChainDailyLimitsAsync();
    }
    
    [HttpGet]
    [Route("rateLimits")]
    public Task<ListResultDto<CrossChainRateLimitsDto>> GetCrossChainRateLimitsAsync()
    {
        return _crossChainLimitInfoAppService.GetCrossChainRateLimitsAsync();
    }
}