using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Filter;
using AElf.CrossChainServer.TokenAccess;
using AElf.CrossChainServer.TokenPool;
using Asp.Versioning;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Application")]
[Route("api/ebridge/application/")]
public class TokenAccessController : CrossChainServerController
{
    private readonly ITokenAccessAppService _tokenAccessAppService;
    private readonly ILiquidityAppService _liquidityAppService;

    public TokenAccessController(ITokenAccessAppService tokenAccessAppService, ILiquidityAppService liquidityAppService)
    {
        _tokenAccessAppService = tokenAccessAppService;
        _liquidityAppService = liquidityAppService;
    }

    [HttpGet("token/config")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<TokenConfigDto> GetTokenConfigAsync(GetTokenConfigInput input)
    {
        return await _tokenAccessAppService.GetTokenConfigAsync(input);
    }


    [HttpGet("token-white-list")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<Dictionary<string, Dictionary<string, TokenInfoDto>>> GetTokenWhitelistAsync()
    {
        return await _tokenAccessAppService.GetTokenWhitelistAsync();
    }

    [Authorize]
    [HttpGet("tokens")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<AvailableTokensDto> GetAvailableTokensAsync(GetAvailableTokensInput input)
    {
        return await _tokenAccessAppService.GetAvailableTokensAsync(input);
    }

    [Authorize]
    [HttpPost("commit-basic-info")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<bool> CommitTokenAccessInfoAsync(UserTokenAccessInfoInput input)
    {
        return await _tokenAccessAppService.CommitTokenAccessInfoAsync(input);
    }

    [Authorize]
    [HttpGet("user-token-access-info")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<UserTokenAccessInfoDto> GetUserTokenAccessInfoAsync(UserTokenAccessInfoBaseInput input)
    {
        return await _tokenAccessAppService.GetUserTokenAccessInfoAsync(input);
    }

    [Authorize]
    [HttpGet("check-chain-access-status")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<CheckChainAccessStatusResultDto> CheckChainAccessStatusAsync(CheckChainAccessStatusInput input)
    {
        return await _tokenAccessAppService.CheckChainAccessStatusAsync(input);
    }

    [Authorize]
    [HttpPost("add-chain")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<AddChainResultDto> AddChainAsync(AddChainInput input)
    {
        return await _tokenAccessAppService.AddChainAsync(input);
    }

    [Authorize]
    [HttpPost("prepare-binding-issue")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<UserTokenBindingDto> PrepareBindingIssueAsync(PrepareBindIssueInput input)
    {
        return await _tokenAccessAppService.PrepareBindingIssueAsync(input);
    }

    [Authorize]
    [HttpPost("issue-binding")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<bool> GetBindingIssueAsync(UserTokenBindingDto input)
    {
        return await _tokenAccessAppService.GetBindingIssueAsync(input);
    }

    [Authorize]
    [HttpGet("list")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<PagedResultDto<TokenApplyOrderDto>> GetTokenApplyOrderListAsync(
        GetTokenApplyOrderListInput input)
    {
        return await _tokenAccessAppService.GetTokenApplyOrderListAsync(input);
    }

    [Authorize]
    [HttpGet("detail")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<List<TokenApplyOrderDto>> GetTokenApplyOrderDetailAsync(GetTokenApplyOrderInput input)
    {
        return await _tokenAccessAppService.GetTokenApplyOrderDetailAsync(input);
    }
    
    [HttpGet("pool-overview")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<PoolOverviewDto> GetPoolOverviewAsync([CanBeNull] string addresses)
    {
        return await _liquidityAppService.GetPoolOverviewAsync(addresses);
    }
    
    [HttpGet("pool-list")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<PagedResultDto<PoolInfoDto>> GetPoolListAsync(GetPoolListInput input)
    {
        return await _liquidityAppService.GetPoolListAsync(input);
    }
    
    [HttpGet("pool-detail")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<PoolInfoDto> GetPoolDetailAsync(GetPoolDetailInput input)
    {
        return await _liquidityAppService.GetPoolDetailAsync(input);
    }
    
    [HttpGet("token/price")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<TokenPriceDto> GetTokenPriceAsync(GetTokenPriceInput input)
    {
        return await _tokenAccessAppService.GetTokenPriceAsync(input);
    }

    [Authorize(Roles = "admin")]
    [HttpPost("trigger-order-status-change")]
    [ServiceFilter(typeof(ResultFilter))]
    public async Task<bool> TriggerOrderStatusChangeAsync(TriggerOrderStatusChangeInput input)
    {
        return await _tokenAccessAppService.TriggerOrderStatusChangeAsync(input);
    }
}