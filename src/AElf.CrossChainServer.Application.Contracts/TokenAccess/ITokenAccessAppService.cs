using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenAccess;

public interface ITokenAccessAppService
{
    Task<AvailableTokensDto> GetAvailableTokensAsync();
    Task<bool> CommitTokenAccessInfoAsync(UserTokenAccessInfoInput input);
    Task<UserTokenAccessInfoDto> GetUserTokenAccessInfoAsync(UserTokenAccessInfoBaseInput input);
    Task<CheckChainAccessStatusResultDto> CheckChainAccessStatusAsync(CheckChainAccessStatusInput input);
    Task<AddChainResultDto> AddChainAsync(AddChainInput input);
    Task<UserTokenBindingDto> PrepareBindingIssueAsync(PrepareBindIssueInput input);
    Task<bool> GetBindingIssueAsync(UserTokenBindingDto input);
    
    Task<PagedResultDto<TokenApplyOrderDto>> GetTokenApplyOrderListAsync(GetTokenApplyOrderListInput input);
    Task<List<TokenApplyOrderDto>> GetTokenApplyOrderDetailAsync(GetTokenApplyOrderInput input);
    
    Task AddUserTokenAccessInfoIndexAsync(AddUserTokenAccessInfoIndexInput input);
    Task UpdateUserTokenAccessInfoIndexAsync(UpdateUserTokenAccessInfoIndexInput input);
    Task AddThirdUserTokenIssueInfoIndexAsync(AddThirdUserTokenIssueInfoIndexInput input);
    Task UpdateThirdUserTokenIssueInfoIndexAsync(UpdateThirdUserTokenIssueInfoIndexInput input);

    Task AddTokenApplyOrderIndexAsync(AddTokenApplyOrderIndexInput input);
    Task UpdateTokenApplyOrderIndexAsync(UpdateTokenApplyOrderIndexInput input);
    
    Task<TokenConfigDto> GetTokenConfigAsync(GetTokenConfigInput input);
    
    Task<Dictionary<string, Dictionary<string, TokenInfoDto>>> GetTokenWhitelistAsync();
    
    Task<PoolOverviewDto> GetPoolOverviewAsync([CanBeNull] string addresses);
    
    Task<PagedResultDto<PoolInfoDto>> GetPoolListAsync(GetPoolListInput input);
    Task<PoolInfoDto> GetPoolDetailAsync(GetPoolDetailInput input);

    Task<TokenPriceDto> GetTokenPriceAsync(GetTokenPriceInput input);

    Task<CommitAddLiquidityDto> CommitAddLiquidityAsync(CommitAddLiquidityInput input);
    
    Task TriggerOrderStatusChangeAsync(TriggerOrderStatusChangeInput input);
}