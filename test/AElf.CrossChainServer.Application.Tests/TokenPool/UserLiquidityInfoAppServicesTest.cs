using System.Threading.Tasks;
using AElf.CrossChainServer.Tokens;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.TokenPool;

public class UserLiquidityInfoAppServiceTests : CrossChainServerApplicationTestBase
{
    private readonly IUserLiquidityInfoAppService _userLiquidityInfoAppService;
    private readonly ITokenAppService _tokenAppService;

    public UserLiquidityInfoAppServiceTests()
    {
        _userLiquidityInfoAppService = GetRequiredService<IUserLiquidityInfoAppService>();
        _tokenAppService = GetRequiredService<ITokenAppService>();
    }
    
    [Fact]
    public async Task AddLiquidityAsync_Test()
    {
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = "MainChain_AELF",
            Symbol = "ELF"
        });
        var input = new UserLiquidityInfoInput
        {
            ChainId = "MainChain_AELF",
            TokenId = token.Id,
            Liquidity = 1,
            Provider = "Provider"
        };
        await _userLiquidityInfoAppService.AddUserLiquidityAsync(input);
        var liq = await _userLiquidityInfoAppService.GetUserLiquidityInfosAsync(new GetUserLiquidityInput(){
            ChainId = "MainChain_AELF",
            Providers = ["Provider"]
        });
        liq.Count.ShouldBe(1);
        liq[0].ChainId.ShouldBe("MainChain_AELF");
        liq[0].TokenInfo.Symbol.ShouldBe(token.Symbol);
        liq[0].Liquidity.ShouldBe(1);
        liq[0].Provider.ShouldBe("Provider");
    }
    
    [Fact]
    public async Task RemoveLiquidityAsync_Test()
    {
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = "MainChain_AELF",
            Symbol = "ELF"
        });
        var input = new UserLiquidityInfoInput
        {
            ChainId = "MainChain_AELF",
            TokenId = token.Id,
            Liquidity = 2,
            Provider = "Provider"
        };
        await _userLiquidityInfoAppService.AddUserLiquidityAsync(input);
        input = new UserLiquidityInfoInput
        {
            ChainId = "MainChain_AELF",
            TokenId = token.Id,
            Liquidity = 1,
            Provider = "Provider"
        };
        await _userLiquidityInfoAppService.RemoveUserLiquidityAsync(input);
        var liq = await _userLiquidityInfoAppService.GetUserLiquidityInfosAsync(new GetUserLiquidityInput(){
            ChainId = "MainChain_AELF",
            Providers = ["Provider"]
        });
        liq.Count.ShouldBe(1);
        liq[0].ChainId.ShouldBe("MainChain_AELF");
        liq[0].TokenInfo.Symbol.ShouldBe(token.Symbol);
        liq[0].Liquidity.ShouldBe(1);
    }
}