using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenAccess;

public class MockAwakenProvider : IAwakenProvider
{
    private readonly Dictionary<string, string> _tokenLiquidityMap = new Dictionary<string, string>();
    private readonly Dictionary<string, decimal> _tokenPriceMap = new Dictionary<string, decimal>();

    public void SetupTokenLiquidityInUsd(string symbol, string liquidityInUsd)
    {
        _tokenLiquidityMap[symbol] = liquidityInUsd;
    }

    public void SetupTokenPrice(string symbol, decimal priceInUsd)
    {
        _tokenPriceMap[symbol] = priceInUsd;
    }

    public Task<string> GetTokenLiquidityInUsdAsync(string symbol)
    {
        if (_tokenLiquidityMap.TryGetValue(symbol, out var liquidity))
        {
            return Task.FromResult(liquidity);
        }
        
        return Task.FromResult("0");
    }

    public Task<decimal> GetTokenPriceInUsdAsync(string symbol)
    {
        if (_tokenPriceMap.TryGetValue(symbol, out var price))
        {
            return Task.FromResult(price);
        }
        
        return Task.FromResult(1.5m); // Default price
    }
}