using System;
using System.Globalization;
using System.Threading.Tasks;
using AElf.CrossChainServer.TokenPrice;
using AElf.ExceptionHandler;
using Aetherlink.PriceServer;
using Aetherlink.PriceServer.Dtos;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace AElf.AetherlinkApi;

public class TokenPriceProvider : ITokenPriceProvider, ISingletonDependency
{
    private readonly IPriceServerProvider _priceServerProvider;

    public TokenPriceProvider(IPriceServerProvider priceServerProvider)
    {
        _priceServerProvider = priceServerProvider;
    }

    [ExceptionHandler(typeof(Exception), Message = "GetPrice Error", LogOnly = true)]
    public async Task<decimal> GetPriceAsync(string pair)
    {
        var result = (await _priceServerProvider.GetAggregatedTokenPriceAsync(new GetAggregatedTokenPriceRequestDto
        {
            TokenPair = pair,
            AggregateType = AggregateType.Latest
        })).Data;

        Log.Information(
            "Get token price from Aetherlink price service, pair: {pair}, price: {price}, decimal: {tokenDecimal}",
            result.TokenPair, result.Price, result.Decimal);

        return (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
    }

    [ExceptionHandler(typeof(Exception), Message = "Get history price error", LogOnly = true)]
    public async Task<decimal> GetHistoryPriceAsync(string pair, string dateTime)
    {
        var date = DateTime.ParseExact(dateTime, "dd-MM-yyyy", CultureInfo.InvariantCulture).ToString("yyyyMMdd");

        var tokenPair = pair;
        var result = (await _priceServerProvider.GetDailyPriceAsync(new GetDailyPriceRequestDto
        {
            TokenPair = tokenPair,
            TimeStamp = date
        })).Data;

        Log.Information(
            "Get history token price from Aetherlink price service, tokenPair: {tokenPair}, TimeStamp: {date}, result.Price: {resultPrice}, result.Decimal: {resultDecimal}",
            tokenPair, date, result.Price, result.Decimal);

        return (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
    }
}