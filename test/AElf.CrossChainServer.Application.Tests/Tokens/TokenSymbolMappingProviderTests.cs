using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.Tokens;

public class TokenSymbolMappingProviderTests: CrossChainServerApplicationTestBase
{
    private readonly ITokenSymbolMappingProvider _tokenSymbolMappingProvider;

    public TokenSymbolMappingProviderTests()
    {
        _tokenSymbolMappingProvider = GetRequiredService<ITokenSymbolMappingProvider>();
    }

    [Fact]
    public async Task Test()
    {
        var symbol = _tokenSymbolMappingProvider.GetMappingSymbol("Ethereum", "MainChain_AELF", "WETH");
        symbol.ShouldBe("ETH");
        
        symbol = _tokenSymbolMappingProvider.GetMappingSymbol("Ethereum", "MainChain_AELF", "USDT");
        symbol.ShouldBe("USDT");
    }
}