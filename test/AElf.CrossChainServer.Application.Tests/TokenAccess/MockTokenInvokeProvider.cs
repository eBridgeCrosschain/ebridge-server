using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;

namespace AElf.CrossChainServer.TokenAccess;

public class MockTokenInvokeProvider : ITokenInvokeProvider
{
    public async Task<bool> GetThirdTokenListAndUpdateAsync(string symbol)
    {
        // Mock implementation that always returns success
        return true;
    }

    public async Task<UserTokenBindingDto> PrepareBindingAsync(ThirdUserTokenIssueInfoDto input)
    {
        return new UserTokenBindingDto
        {
            BindingId = "binding_id",
            ThirdTokenId = "third_token_id",
            MintToAddress = input.WalletAddress
        };
    }

    public async Task<bool> BindingAsync(UserTokenBindingDto input)
    {
        return true;
    }
} 