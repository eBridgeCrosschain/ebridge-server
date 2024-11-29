using System;
using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenAccess;

public interface ITokenAccessAppService
{
    Task<AvailableTokensDto> GetAvailableTokensAsync(string address);
    Task CommitTokenAccessInfoAsync(UserTokenAccessInfoInput input);
    Task<UserTokenAccessInfoDto> GetUserTokenAccessInfoAsync(UserTokenAccessInfoInput input);
    Task<CheckChainAccessStatusResultDto> CheckChainAccessStatusAsync(string symbol, string address);
    Task SelectChainAsync(SelectChainInput input);
    Task IssueTokenAsync(IssueTokenInput input);
    Task<TokenApplyOrderListDto> GetTokenApplyOrderList(GetTokenApplyOrderListInput input);
    Task<TokenApplyOrderDto> GetTokenApplyOrder(Guid id);
}