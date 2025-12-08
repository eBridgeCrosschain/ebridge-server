using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.TokenAccess;

public interface IAwakenProvider
{
    Task<string> GetTokenLiquidityInUsdAsync(string symbol);
    Task<decimal> GetTokenPriceInUsdAsync(string symbol);

}

public class AwakenProvider : IAwakenProvider , ITransientDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly TokenAccessOptions _tokenAccessOptions;

    public AwakenProvider(IHttpProvider httpProvider,IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions)
    {
        _httpProvider = httpProvider;
        _tokenAccessOptions = tokenAccessOptions.Value;
    }

    private ApiInfo TokenLiquidityUri => new(HttpMethod.Get, _tokenAccessOptions.AwakenGetTokenLiquidityUri);
    private ApiInfo TokenPriceUri => new(HttpMethod.Get, _tokenAccessOptions.AwakenGetPriceUri);


    public async Task<string> GetTokenLiquidityInUsdAsync(string symbol)
    {
        var tokenParams = new Dictionary<string, string>();
        tokenParams["symbol"] = symbol;
        var resultDto = await _httpProvider.InvokeAsync<CommonResponseDto<string>>(_tokenAccessOptions.AwakenBaseUrl,
            TokenLiquidityUri, param: tokenParams);
        return resultDto.Code == CrossChainServerConsts.SuccessHttpCode ? resultDto.Value : "0";
    }

    public async Task<decimal> GetTokenPriceInUsdAsync(string symbol)
    {
        if (_tokenAccessOptions.SymbolMap.TryGetValue(symbol, out var symbolMap))
        {
            symbol = symbolMap;
        }
        var tokenParams = new Dictionary<string, string>();
        tokenParams["symbol"] = symbol;
        var resultDto = await _httpProvider.InvokeAsync<CommonResponseDto<string>>(_tokenAccessOptions.AwakenBaseUrl,
            TokenPriceUri, param: tokenParams);
        return resultDto.Code == CrossChainServerConsts.SuccessHttpCode ? decimal.Parse(resultDto.Value) : 0;
    }
}