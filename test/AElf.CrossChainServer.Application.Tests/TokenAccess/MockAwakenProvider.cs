using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenAccess;

public class MockAwakenProvider :IAwakenProvider
{
    public async Task<string> GetTokenLiquidityInUsdAsync(string symbol)
    {
        return "1.5";
    }

    public async Task<decimal> GetTokenPriceInUsdAsync(string symbol)
    {
        return 1.5m;
    }
}