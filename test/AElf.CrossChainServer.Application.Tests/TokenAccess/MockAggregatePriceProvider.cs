using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenAccess;

public class MockAggregatePriceProvider :IAggregatePriceProvider
{
    public async Task<decimal> GetPriceAsync(string symbol)
    {
        return 1.2m;
    }
}