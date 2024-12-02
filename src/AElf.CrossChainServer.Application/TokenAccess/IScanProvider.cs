using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.HttpClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;

namespace AElf.CrossChainServer.TokenAccess;

public interface IScanProvider
{
    Task<List<AvailableTokenDto>> GetOwnTokensAsync(string address);
}

public class ScanProvider : IScanProvider
{
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly IDistributedCache<List<AvailableTokenDto>> _distributedCache;
    private readonly AElfClientProvider _aElfClientProvider;
    private const string CachePrefix = "EbridgeServer:OwnerTokenList:";
    private ApiInfo _tokenListUri => new(HttpMethod.Get, _tokenAccessOptions.ScanTokenListUri);

    public ScanProvider(IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions, IHttpProvider httpProvider, IDistributedCache<List<AvailableTokenDto>> distributedCache, 
        AElfClientProvider aElfClientProvider)
    {
        _tokenAccessOptions = tokenAccessOptions.Value;
        _httpProvider = httpProvider;
        _distributedCache = distributedCache;
        _aElfClientProvider = aElfClientProvider;
    }

    public async Task RefreshCacheAsync()
    {
        var pathParams = new Dictionary<string, string>();
        pathParams["maxResultCount"] = "1000";
        var resultDto = await _httpProvider.InvokeAsync<ScanTokenListResultDto>(_tokenAccessOptions.ScanBaseUrl, _tokenListUri, pathParams);
        if (resultDto.Code != "20000" && resultDto.Data.List.Count < 0)
        {
            return;
        }

        var ownerDictionary = new Dictionary<string, List<AvailableTokenDto>>();
        foreach (var token in resultDto.Data.List)
        {
            var tokenInfo = await _aElfClientProvider.GetTokenAsync(CrossChainServerConsts.AElfMainChainId, null, token.Token.Symbol);
            if (!ownerDictionary.TryGetValue(tokenInfo.Owner, out var tokenList))
            {
                tokenList = new List<AvailableTokenDto>();
                ownerDictionary[tokenInfo.Owner] = tokenList;
            }
            tokenList.Add(new ()
            {
                Holders = token.Holders,
                Symbol = token.Token.Symbol,
                TokenName = token.Token.Name,
            });
        }

        foreach (var entry in ownerDictionary)
        {
            await _distributedCache.SetAsync(GetCacheKey(entry.Key), entry.Value, new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMonths(1)
            });
        }
    }

    public async Task<List<AvailableTokenDto>> GetOwnTokensAsync(string address)
    {
        return await _distributedCache.GetAsync(GetCacheKey(address));
    }

    private string GetCacheKey(string key)
    {
        return $"{CachePrefix}:{key}";
    }
}

public class ScanTokenListResultDto
{
    public string Code { get; set; }
    public ScanDataDto Data { get; set; }
}

public class ScanDataDto {
    public List<ScanTokenItem> List { get; set; }
}

public class ScanTokenItem
{
    public int Holders { get; set; }
    public Token Token { get; set; }
}

public class Token
{
    public string Name { get; set; }
    public string Symbol { get; set; }
}