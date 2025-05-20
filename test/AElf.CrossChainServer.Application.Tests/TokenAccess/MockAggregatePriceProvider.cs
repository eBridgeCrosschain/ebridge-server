using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenAccess;

public class MockAggregatePriceProvider : IAggregatePriceProvider
{
    private readonly Dictionary<string, decimal> _tokenPriceMap = new Dictionary<string, decimal>();

    public void SetupTokenPrice(string symbol, decimal priceInUsd)
    {
        _tokenPriceMap[symbol] = priceInUsd;
    }

    public Task<decimal> GetPriceAsync(string symbol)
    {
        if (_tokenPriceMap.TryGetValue(symbol, out var price))
        {
            return Task.FromResult(price);
        }
        
        return Task.FromResult(1m); // Default price
    }
}