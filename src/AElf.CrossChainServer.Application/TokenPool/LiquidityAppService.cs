using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.TokenAccess;
using AElf.CrossChainServer.Tokens;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenPool;

[RemoteService(IsEnabled = false)]
public class LiquidityAppService : CrossChainServerAppService, ILiquidityAppService
{
    private readonly IPoolLiquidityInfoAppService _poolLiquidityInfoAppService;
    private readonly IUserLiquidityInfoAppService _userLiquidityInfoAppService;
    private readonly IChainAppService _chainAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly IAggregatePriceProvider _aggregatePriceProvider;
    private readonly ITokenImageProvider _tokenImageProvider;
    private const int MaxMaxResultCount = 1000;
    private const int DefaultSkipCount = 0;

    private readonly TokenAccessOptions _tokenAccessOptions;

    public LiquidityAppService(IPoolLiquidityInfoAppService poolLiquidityInfoAppService,
        IUserLiquidityInfoAppService userLiquidityInfoAppService,
        IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions, IAggregatePriceProvider aggregatePriceProvider,
        ITokenImageProvider tokenImageProvider, IChainAppService chainAppService, ITokenAppService tokenAppService)
    {
        _poolLiquidityInfoAppService = poolLiquidityInfoAppService;
        _userLiquidityInfoAppService = userLiquidityInfoAppService;
        _aggregatePriceProvider = aggregatePriceProvider;
        _tokenImageProvider = tokenImageProvider;
        _chainAppService = chainAppService;
        _tokenAppService = tokenAppService;
        _tokenAccessOptions = tokenAccessOptions.Value;
    }

    public async Task<PoolOverviewDto> GetPoolOverviewAsync(string addresses)
    {
        var poolLiquidityInfoList = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(
            new GetPoolLiquidityInfosInput
            {
                MaxResultCount = MaxMaxResultCount,
                SkipCount = DefaultSkipCount
            });
        var blackSymbolList = _tokenAccessOptions.BlackSymbolList;
        var poolList = new List<PoolLiquidityIndexDto>();
        if (blackSymbolList.Count > 0)
        {
            poolList.AddRange(poolLiquidityInfoList.Items.ToList().Where(r => !blackSymbolList.Contains(r.TokenInfo.Symbol)));
        }
        var poolCount = poolList.Count;
        var symbolList = poolList
            .Select(p => p.TokenInfo.Symbol)
            .Select(symbol =>
                _tokenAccessOptions.SymbolMap.TryGetValue(symbol, out var aelfSymbol) ? aelfSymbol : symbol)
            .Distinct()
            .ToList();
        var tokenCount = symbolList.Count;
        var userLiquidityInfoList = string.IsNullOrWhiteSpace(addresses)
            ? new List<UserLiquidityIndexDto>()
            : await _userLiquidityInfoAppService.GetUserLiquidityInfosAsync(new GetUserLiquidityInput
                { Providers = addresses.Split(',').ToList() });
        var tokenPrice = await GetTokenPricesAsync(poolList);
        var totalLiquidityInUsd = CalculateTotalLiquidityInUsd(poolList, tokenPrice);
        var totalMyLiquidityInUsd = CalculateUserTotalLiquidityInUsd(userLiquidityInfoList, tokenPrice);
        return new PoolOverviewDto
        {
            MyTotalTvlInUsd = totalMyLiquidityInUsd,
            TotalTvlInUsd = totalLiquidityInUsd,
            PoolCount = poolCount,
            TokenCount = tokenCount
        };
    }

    public async Task<PagedResultDto<PoolInfoDto>> GetPoolListAsync(GetPoolListInput input)
    {
        var poolLiquidityInfos = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(
            new GetPoolLiquidityInfosInput
            {
                MaxResultCount = input.MaxResultCount,
                SkipCount = input.SkipCount
            });
        var poolLiquidityInfoList = poolLiquidityInfos.Items.ToList();
        var result = new List<PoolInfoDto>();
        var tokenPrice = await GetTokenPricesAsync(poolLiquidityInfoList);
        var groupedPoolLiquidityInfoList = poolLiquidityInfoList.GroupBy(p => p.ChainId);

        foreach (var group in groupedPoolLiquidityInfoList)
        {
            var chainId = group.Key;
            foreach (var poolLiquidity in group)
            {
                var tokenPriceInUsd = tokenPrice[poolLiquidity.TokenInfo.Symbol];
                poolLiquidity.TokenInfo.Icon =
                    await _tokenImageProvider.GetTokenImageAsync(poolLiquidity.TokenInfo.Symbol);
                var poolInfo = new PoolInfoDto
                {
                    ChainId = poolLiquidity.ChainId,
                    Token = poolLiquidity.TokenInfo,
                    TotalTvlInUsd = poolLiquidity.Liquidity * tokenPriceInUsd,
                    TokenPrice = tokenPriceInUsd,
                    MyTvlInUsd = 0
                };
                if (!string.IsNullOrWhiteSpace(input.Addresses))
                {
                    var userLiquidityInfo = await _userLiquidityInfoAppService.GetUserLiquidityInfosAsync(
                        new GetUserLiquidityInput
                        {
                            Providers = input.Addresses.Split(',').ToList(),
                            ChainId = chainId,
                            Symbol = poolLiquidity.TokenInfo.Symbol
                        });
                    if (userLiquidityInfo?.Count > 0)
                    {
                        var userLiquidityTotal = userLiquidityInfo.Sum(l => l.Liquidity);
                        poolInfo.MyTvlInUsd = userLiquidityTotal * tokenPriceInUsd;
                    }
                }

                result.Add(poolInfo);
            }
        }

        DealWithBlackList(result);
        return new PagedResultDto<PoolInfoDto>
        {
            TotalCount = poolLiquidityInfos.TotalCount,
            Items = result.OrderByDescending(r => r.TotalTvlInUsd).ThenBy(r => r.Token.Symbol).ToList()
        };
    }

    private void DealWithBlackList(List<PoolInfoDto> result)
    {
        var blackSymbolList = _tokenAccessOptions.BlackSymbolList;
        if (blackSymbolList.Count == 0)
        {
            return;
        }

        result.RemoveAll(r => blackSymbolList.Contains(r.Token.Symbol));
    }

    public async Task<PoolInfoDto> GetPoolDetailAsync(GetPoolDetailInput input)
    {
        var poolLiquidityInfos = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(
            new GetPoolLiquidityInfosInput
            {
                ChainId = input.ChainId,
                Token = input.Token
            });
        var poolLiquidity = poolLiquidityInfos.Items.FirstOrDefault();
        if (poolLiquidity == null)
        {
            Log.Warning("Pool liquidity info not found.{chainId} {token}", input.ChainId, input.Token);
            return new PoolInfoDto();
        }

        var chainType = await _chainAppService.GetAsync(input.ChainId);
        string tokenSymbol;
        if (chainType.Type == BlockchainType.AElf)
        {
            tokenSymbol = input.Token;
        }
        else
        {
            tokenSymbol = (await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = input.ChainId,
                Address = input.Token
            })).Symbol;
        }

        var priceInUsd = await _aggregatePriceProvider.GetPriceAsync(tokenSymbol);
        var myLiquidity = 0m;
        if (!string.IsNullOrWhiteSpace(input.Address))
        {
            var userLiq = await _userLiquidityInfoAppService.GetUserLiquidityInfosAsync(new GetUserLiquidityInput
            {
                Providers = new List<string>() { input.Address },
                ChainId = input.ChainId,
                Symbol = tokenSymbol
            });
            myLiquidity = userLiq.FirstOrDefault()?.Liquidity ?? 0;
        }

        poolLiquidity.TokenInfo.Icon = await _tokenImageProvider.GetTokenImageAsync(poolLiquidity.TokenInfo.Symbol);
        return new PoolInfoDto
        {
            ChainId = poolLiquidity.ChainId,
            Token = poolLiquidity.TokenInfo,
            TotalTvlInUsd = poolLiquidity.Liquidity * priceInUsd,
            MyTvlInUsd = myLiquidity * priceInUsd,
            TokenPrice = priceInUsd
        };
    }

    private async Task<Dictionary<string, decimal>> GetTokenPricesAsync(
        List<PoolLiquidityIndexDto> poolLiquidityInfoList)
    {
        var allSymbols = poolLiquidityInfoList
            .Select(pool => pool.TokenInfo.Symbol)
            .Distinct()
            .ToList();
        var tokenPrices = new Dictionary<string, decimal>();
        foreach (var symbol in allSymbols)
        {
            Log.Debug("To get token price: {symbol}", symbol);
            var priceInUsd = await _aggregatePriceProvider.GetPriceAsync(symbol);
            tokenPrices[symbol] = priceInUsd;
        }

        return tokenPrices;
    }

    private decimal CalculateTotalLiquidityInUsd(
        List<PoolLiquidityIndexDto> poolLiquidityInfoList,
        Dictionary<string, decimal> tokenPrices)
    {
        if (poolLiquidityInfoList.Count == 0)
        {
            return 0m;
        }

        return poolLiquidityInfoList
            .GroupBy(pool => pool.TokenInfo.Symbol)
            .Sum(group =>
            {
                var priceInUsd = tokenPrices[group.Key];
                var totalLiquidity = group.Sum(pool => pool.Liquidity);
                return totalLiquidity * priceInUsd;
            });
    }

    private decimal CalculateUserTotalLiquidityInUsd(
        List<UserLiquidityIndexDto> userLiquidityInfoList,
        Dictionary<string, decimal> tokenPrices)
    {
        if (userLiquidityInfoList.Count == 0)
        {
            return 0m;
        }

        return userLiquidityInfoList
            .GroupBy(userLiq => userLiq.TokenInfo.Symbol)
            .Sum(group =>
            {
                var priceInUsd = tokenPrices[group.Key];
                var totalLiquidity = group.Sum(userLiq => userLiq.Liquidity);
                return totalLiquidity * priceInUsd;
            });
    }
}