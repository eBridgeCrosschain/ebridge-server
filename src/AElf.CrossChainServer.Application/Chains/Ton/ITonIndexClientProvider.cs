using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.Client.Service;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.Chains;

public interface ITonIndexClientProvider
{
    Task<T> GetAsync<T>(string chainId, string path);
}

public class TonIndexClientProvider : ITonIndexClientProvider, ISingletonDependency
{
    private readonly ChainApiOptions _chainApiOptions;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ApiKeyOptions _apiKeyOptions;

    public TonIndexClientProvider(IOptionsSnapshot<ChainApiOptions> apiOptions, IHttpClientFactory clientFactory,IOptionsSnapshot<ApiKeyOptions> apiKeyOptions)
    {
        _clientFactory = clientFactory;
        _chainApiOptions = apiOptions.Value;
        _apiKeyOptions = apiKeyOptions.Value;
    }

    private System.Net.Http.HttpClient GetClient()
    {
        var client = _clientFactory.CreateClient();
        if (!string.IsNullOrEmpty(_apiKeyOptions.TonIndex))
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", _apiKeyOptions.TonIndex);
        }
        return client;
    }

    public async Task<T> GetAsync<T>(string chainId, string path)
    {
        var client = GetClient();
        var uri = _chainApiOptions.ChainNodeApis[chainId].TrimEnd('/') + path;
        var resp = await client.GetAsync(uri);
        return await resp.Content.DeserializeSnakeCaseAsync<T>();
    }
}