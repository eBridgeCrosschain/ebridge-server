using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;

namespace AElf.CrossChainServer.TokenPool;

public interface ITokenLiquidityCacheProvider
{
    Task AddTokenLiquidityCacheAsync(string orderId, string symbol, string chainId, decimal liquidity);
    Task<OrderLiquidityInfo> GetTokenLiquidityCacheAsync(string symbol, string chainId);
    
}

public class OrderLiquidityInfo
{
    public string Liquidity { get; set; }
    public string OrderId { get; set; }
}
public class TokenLiquidityCacheProvider : ITokenLiquidityCacheProvider
{
    private readonly IDistributedCache<OrderLiquidityInfo> _distributedCache;
    private const string CachePrefix = "EbridgeServer:LiquidityCache:";


    public TokenLiquidityCacheProvider(IDistributedCache<OrderLiquidityInfo> distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task AddTokenLiquidityCacheAsync(string orderId,string symbol, string chainId, decimal liquidity)
    {
        var key = GetCacheKey(symbol, chainId);
        var value = new OrderLiquidityInfo
        {
            OrderId = orderId,
            Liquidity = liquidity.ToString()
        };
        await _distributedCache.SetAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddDays(1)
        });
    }

    public async Task<OrderLiquidityInfo> GetTokenLiquidityCacheAsync(string symbol, string chainId)
    {
        var key = GetCacheKey(symbol, chainId);
        var orderLiquidityInfo = await _distributedCache.GetAsync(key);
        return orderLiquidityInfo;
    }

    private string GetCacheKey(string symbol, string chainId)
    {
        return $"{CachePrefix}:{chainId}-{symbol}";
    }
}