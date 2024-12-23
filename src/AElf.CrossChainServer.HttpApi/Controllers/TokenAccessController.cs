using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.TokenAccess;
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

    public TokenAccessController(ITokenAccessAppService tokenAccessAppService)
    {
        _tokenAccessAppService = tokenAccessAppService;
    }

    [HttpGet("token/config")]
    public async Task<TokenConfigDto> GetTokenConfigAsync(GetTokenConfigInput input)
    {
        return await _tokenAccessAppService.GetTokenConfigAsync(input);
    }


    [HttpGet("token-white-list")]
    public async Task<TokenWhitelistDto> GetTokenWhitelistAsync()
    {
        return await _tokenAccessAppService.GetTokenWhitelistAsync();
    }

    [Authorize]
    [HttpGet("tokens")]
    public async Task<AvailableTokensDto> GetAvailableTokensAsync()
    {
        return await _tokenAccessAppService.GetAvailableTokensAsync();
    }

    [Authorize]
    [HttpPost("commit-basic-info")]
    public async Task<bool> CommitTokenAccessInfoAsync(UserTokenAccessInfoInput input)
    {
        return await _tokenAccessAppService.CommitTokenAccessInfoAsync(input);
    }

    [Authorize]
    [HttpGet("user-token-access-info")]
    public async Task<UserTokenAccessInfoDto> GetUserTokenAccessInfoAsync(UserTokenAccessInfoBaseInput input)
    {
        return await _tokenAccessAppService.GetUserTokenAccessInfoAsync(input);
    }

    [Authorize]
    [HttpGet("check-chain-access-status")]
    public async Task<CheckChainAccessStatusResultDto> CheckChainAccessStatusAsync(CheckChainAccessStatusInput input)
    {
        return await _tokenAccessAppService.CheckChainAccessStatusAsync(input);
    }

    [Authorize]
    [HttpPost("add-chain")]
    public async Task<AddChainResultDto> AddChainAsync(AddChainInput input)
    {
        return await _tokenAccessAppService.AddChainAsync(input);
    }

    [Authorize]
    [HttpPost("prepare-binding-issue")]
    public async Task<UserTokenBindingDto> PrepareBindingIssueAsync(PrepareBindIssueInput input)
    {
        return await _tokenAccessAppService.PrepareBindingIssueAsync(input);
    }

    [Authorize]
    [HttpGet("issue-binding")]
    public async Task<bool> GetBindingIssueAsync(UserTokenBindingDto input)
    {
        return await _tokenAccessAppService.GetBindingIssueAsync(input);
    }

    [Authorize]
    [HttpGet("list")]
    public async Task<PagedResultDto<TokenApplyOrderResultDto>> GetTokenApplyOrderListAsync(
        GetTokenApplyOrderListInput input)
    {
        return await _tokenAccessAppService.GetTokenApplyOrderListAsync(input);
    }

    [Authorize]
    [HttpGet("detail")]
    public async Task<List<TokenApplyOrderResultDto>> GetTokenApplyOrderDetailAsync(GetTokenApplyOrderInput input)
    {
        return await _tokenAccessAppService.GetTokenApplyOrderDetailAsync(input);
    }
    
    [HttpGet("pool-overview")]
    public async Task<PoolOverviewDto> GetPoolOverviewAsync([CanBeNull] string address)
    {
        return await _tokenAccessAppService.GetPoolOverviewAsync(address);
    }
    
    [HttpGet("pool-list")]
    public async Task<PagedResultDto<PoolInfoDto>> GetPoolListAsync(GetPoolListInput input)
    {
        return await _tokenAccessAppService.GetPoolListAsync(input);
    }
    
    [HttpGet("pool-detail")]
    public async Task<PoolInfoDto> GetPoolDetailAsync(GetPoolDetailInput input)
    {
        return await _tokenAccessAppService.GetPoolDetailAsync(input);
    }
    
    [HttpGet("token/price")]
    public async Task<TokenPriceDto> GetTokenPriceAsync(GetTokenPriceInput input)
    {
        return await _tokenAccessAppService.GetTokenPriceAsync(input);
    }
    
    [HttpPost("add-liquidity")]
    public async Task<CommitAddLiquidityDto> CommitAddLiquidityAsync(CommitAddLiquidityInput input)
    {
        return await _tokenAccessAppService.CommitAddLiquidityAsync(input);
    }
}