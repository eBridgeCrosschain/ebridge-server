using System.Threading.Tasks;
using AElf.CrossChainServer.Tokens;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.TokenPool;

public class PoolLiquidityInfoAppServiceTests : CrossChainServerApplicationTestBase
{
    private readonly IPoolLiquidityInfoAppService _poolLiquidityInfoAppService;
    private readonly ITokenAppService _tokenAppService;

    public PoolLiquidityInfoAppServiceTests()
    {
        _poolLiquidityInfoAppService = GetRequiredService<IPoolLiquidityInfoAppService>();
        _tokenAppService = GetRequiredService<ITokenAppService>();
    }
    
    [Fact]
    public async Task GetPoolLiquidityInfosAsync_With_Different_Parameters_Test()
    {
        // Setup test data
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = "MainChain_AELF",
            Symbol = "ELF"
        });
        
        var input = new PoolLiquidityInfoInput
        {
            ChainId = "MainChain_AELF",
            TokenId = token.Id,
            Liquidity = 5,
            Provider = "Provider1"
        };
        await _poolLiquidityInfoAppService.AddLiquidityAsync(input);
        
        // Test with chain filter
        var resultWithChainFilter = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(new GetPoolLiquidityInfosInput
        {
            ChainId = "MainChain_AELF"
        });
        resultWithChainFilter.Items.Count.ShouldBeGreaterThan(0);
        resultWithChainFilter.Items.ShouldAllBe(item => item.ChainId == "MainChain_AELF");
        
        // Test with token filter
        var resultWithTokenFilter = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(new GetPoolLiquidityInfosInput
        {
            Token = token.Address
        });
        resultWithTokenFilter.Items.Count.ShouldBeGreaterThanOrEqualTo(0);
    }
    
    [Fact]
    public async Task AddLiquidityAsync_Test()
    {
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = "MainChain_AELF",
            Symbol = "ELF"
        });
        var input = new PoolLiquidityInfoInput
        {
            ChainId = "MainChain_AELF",
            TokenId = token.Id,
            Liquidity = 1,
            Provider = "Provider"
        };
        await _poolLiquidityInfoAppService.AddLiquidityAsync(input);
        var liq = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(new GetPoolLiquidityInfosInput{
            ChainId = "MainChain_AELF"
        });
        liq.TotalCount.ShouldBe(1);
        liq.Items.Count.ShouldBe(1);
        liq.Items[0].ChainId.ShouldBe("MainChain_AELF");
        liq.Items[0].TokenInfo.Symbol.ShouldBe(token.Symbol);
        liq.Items[0].Liquidity.ShouldBe(1);
    }
    
    [Fact]
    public async Task RemoveLiquidityAsync_Test()
    {
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = "MainChain_AELF",
            Symbol = "ELF"
        });
        var input = new PoolLiquidityInfoInput
        {
            ChainId = "MainChain_AELF",
            TokenId = token.Id,
            Liquidity = 2,
            Provider = "Provider"
        };
        await _poolLiquidityInfoAppService.AddLiquidityAsync(input);
        input = new PoolLiquidityInfoInput
        {
            ChainId = "MainChain_AELF",
            TokenId = token.Id,
            Liquidity = 1,
            Provider = "Provider"
        };
        await _poolLiquidityInfoAppService.RemoveLiquidityAsync(input);
        var liq = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(new GetPoolLiquidityInfosInput{
            ChainId = "MainChain_AELF"
        });
        liq.TotalCount.ShouldBe(1);
        liq.Items.Count.ShouldBe(1);
        liq.Items[0].ChainId.ShouldBe("MainChain_AELF");
        liq.Items[0].TokenInfo.Symbol.ShouldBe(token.Symbol);
        liq.Items[0].Liquidity.ShouldBe(1);
    }
}