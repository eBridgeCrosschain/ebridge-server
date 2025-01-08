using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;

namespace AElf.CrossChainServer.TokenAccess;

public interface ITokenInfoCacheProvider
{
    Task AddTokenListAsync(List<UserTokenInfoDto> userTokenInfoList);
    Task<UserTokenInfoDto> GetTokenAsync(string symbol);
}

public class TokenInfoCacheProvider : ITokenInfoCacheProvider
{
    private readonly IDistributedCache<UserTokenInfoDto> _tokenCache;
    private const string CachePrefix = "EbridgeServer:TokenList:";
    public TokenInfoCacheProvider(IDistributedCache<UserTokenInfoDto> tokenCache)
    {
        _tokenCache = tokenCache;
    }
    public async Task AddTokenListAsync(List<UserTokenInfoDto> userTokenInfoList)
    {
        foreach (var info in userTokenInfoList)
        {
            await _tokenCache.SetAsync(GetCacheKey(info.Symbol), info, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddDays(7)
            });
        }
    }
    public async Task<UserTokenInfoDto> GetTokenAsync(string symbol)
    {
        var key = GetCacheKey(symbol);
        var info = await _tokenCache.GetAsync(key);
        return info ?? new UserTokenInfoDto();
    }
    private string GetCacheKey(string key)
    {
        return $"{CachePrefix}:{key}";
    }
}