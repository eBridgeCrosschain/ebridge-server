using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;
using AElf.CrossChainServer.TokenPrice;
using AElf.CrossChainServer.Tokens;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.TokenAccess;

public interface IAggregatePriceProvider
{
    Task<decimal> GetPriceAsync(string symbol);
}

public class AggregatePriceProvider : IAggregatePriceProvider,ITransientDependency
{
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly TokenPriceIdMappingOptions _tokenPriceIdMappingOptions;
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly IHttpProvider _httpProvider;
    private ApiInfo TokenLiquidityUri => new(HttpMethod.Get, _tokenAccessOptions.AwakenGetPriceUri);

    public AggregatePriceProvider(ITokenPriceProvider tokenPriceProvider,
        IOptionsSnapshot<TokenPriceIdMappingOptions> tokenPriceIdMappingOptions, IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions,
        IHttpProvider httpProvider)
    {
        _tokenPriceProvider = tokenPriceProvider;
        _tokenAccessOptions = tokenAccessOptions.Value;
        _httpProvider = httpProvider;
        _tokenPriceIdMappingOptions = tokenPriceIdMappingOptions.Value;
    }


    public async Task<decimal> GetPriceAsync(string symbol)
    {
        var priceInUsd = 0m;
        Log.Debug("To get token price from aetherlink: {symbol}", symbol);
        if (_tokenPriceIdMappingOptions.CoinIdMapping.TryGetValue(symbol, out var coinId))
        {
            priceInUsd = await _tokenPriceProvider.GetPriceAsync(coinId);
            Log.Debug("Token price from aetherlink: {symbol} {priceInUsd}", symbol, priceInUsd);
        }

        if (priceInUsd != 0)
        {
            return priceInUsd;
        }

        Log.Debug("To get token price from awaken: {symbol}", symbol);
        priceInUsd = await GetPriceFromAwakenAsync(symbol);
        Log.Debug("Token price from awaken: {symbol} {priceInUsd}", symbol, priceInUsd);

        return priceInUsd;
    }

    private async Task<decimal> GetPriceFromAwakenAsync(string symbol)
    {
        if (_tokenAccessOptions.SymbolMap.TryGetValue(symbol, out var symbolMap))
        {
            symbol = symbolMap;
        }
        var tokenParams = new Dictionary<string, string>();
        tokenParams["symbol"] = symbol;
        var resultDto = await _httpProvider.InvokeAsync<CommonResponseDto<string>>(_tokenAccessOptions.AwakenBaseUrl,
            TokenLiquidityUri, param: tokenParams);
        return resultDto.Code == "20000" ? decimal.Parse(resultDto.Value) : 0;
    }
}