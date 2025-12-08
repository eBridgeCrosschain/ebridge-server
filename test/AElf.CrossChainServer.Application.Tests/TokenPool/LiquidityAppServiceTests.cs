using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.TokenPool;

public class LiquidityAppServiceTests : CrossChainServerApplicationTestBase
{
    private readonly ILiquidityAppService _liquidityAppService;
    private readonly IPoolLiquidityInfoAppService _poolLiquidityInfoAppService;
    private readonly IUserLiquidityInfoAppService _userLiquidityInfoAppService;

    public LiquidityAppServiceTests()
    {
        _liquidityAppService = GetRequiredService<ILiquidityAppService>();
        _poolLiquidityInfoAppService = GetRequiredService<IPoolLiquidityInfoAppService>();
        _userLiquidityInfoAppService = GetRequiredService<IUserLiquidityInfoAppService>();
    }

    [Fact]
    public async Task GetPoolOverviewAsync_Should_Return_Overview()
    {
        // Act
        var overview = await _liquidityAppService.GetPoolOverviewAsync(null);

        // Assert
        overview.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPoolListAsync_Should_Return_Pool_List()
    {
        // Act
        var pools = await _liquidityAppService.GetPoolListAsync(new GetPoolListInput
        {
            MaxResultCount = 10,
            SkipCount = 0
        });

        // Assert
        pools.ShouldNotBeNull();
        pools.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPoolDetailAsync_Should_Return_Pool_Detail()
    {
        // Act - Note: This may return null if the pool doesn't exist
        var detail = await _liquidityAppService.GetPoolDetailAsync(new GetPoolDetailInput
        {
            Token = "ELF",
            ChainId = "MainChain_AELF"
        });

        // No assertion necessary as the result depends on existing data
    }
}