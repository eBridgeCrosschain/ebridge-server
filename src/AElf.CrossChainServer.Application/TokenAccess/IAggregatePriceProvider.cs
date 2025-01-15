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

public class AggregatePriceProvider : IAggregatePriceProvider, ITransientDependency
{
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly TokenPriceIdMappingOptions _tokenPriceIdMappingOptions;
    private readonly IAwakenProvider _awakenProvider;

    public AggregatePriceProvider(ITokenPriceProvider tokenPriceProvider,
        IOptionsSnapshot<TokenPriceIdMappingOptions> tokenPriceIdMappingOptions,
        IAwakenProvider awakenProvider)
    {
        _tokenPriceProvider = tokenPriceProvider;
        _awakenProvider = awakenProvider;
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
        priceInUsd = await _awakenProvider.GetTokenPriceInUsdAsync(symbol);
        Log.Debug("Token price from awaken: {symbol} {priceInUsd}", symbol, priceInUsd);

        return priceInUsd;
    }
}