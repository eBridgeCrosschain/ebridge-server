using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;
using Microsoft.Extensions.Options;
using Serilog;

namespace AElf.CrossChainServer.TokenAccess;

public interface ILiquidityDataProvider
{
    Task<string> GetTokenTvlAsync(string symbol);
}

public class LiquidityDataProvider : ILiquidityDataProvider
{
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly IHttpProvider _httpProvider;
    private ApiInfo _tokenLiquidityUri => new(HttpMethod.Get, _tokenAccessOptions.AwakenGetTokenLiquidityUri);

    public LiquidityDataProvider(IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions, IHttpProvider httpProvider)
    {
        _tokenAccessOptions = tokenAccessOptions.Value;
        _httpProvider = httpProvider;
    }

    public async Task<string> GetTokenTvlAsync(string symbol)
    {
        var pathParams = new Dictionary<string, string>();
        pathParams["symbol"] = symbol;
        var resultDto = await _httpProvider.InvokeAsync<ApiCommonResult<string>>(_tokenAccessOptions.AwakenBaseUrl, _tokenLiquidityUri, pathParams);
        if (resultDto.Code != "20000")
        {
            Log.Error("Get token tvl fail: code {code}, message: {message}", resultDto.Code, resultDto.Message);
            return "0";
        }
        return resultDto.Data;
    }
}