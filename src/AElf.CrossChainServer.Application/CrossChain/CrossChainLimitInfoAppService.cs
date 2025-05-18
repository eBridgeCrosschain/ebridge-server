using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Tokens;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.CrossChain;

[RemoteService(IsEnabled = false)]
public class CrossChainLimitInfoAppService : CrossChainServerAppService, ICrossChainLimitInfoAppService
{
    private readonly IIndexerCrossChainLimitInfoService _indexerCrossChainLimitInfoService;
    private readonly IOptionsMonitor<EvmTokensOptions> _evmTokensOptions;
    private readonly ITokenAppService _tokenAppService;
    private readonly IChainAppService _chainAppService;
    private readonly IOptionsMonitor<CrossChainLimitsOptions> _crossChainLimitsOptions;
    private readonly ITokenSymbolMappingProvider _tokenSymbolMappingProvider;
    private readonly ICrossChainLimitAppService _crossChainLimitAppService;

    public CrossChainLimitInfoAppService(
        IIndexerCrossChainLimitInfoService indexerCrossChainLimitInfoService,
        IOptionsMonitor<EvmTokensOptions> evmTokensOptions, ITokenAppService tokenAppService,
        IChainAppService chainAppService,
        IOptionsMonitor<CrossChainLimitsOptions> crossChainLimitsOptions,
        ITokenSymbolMappingProvider tokenSymbolMappingProvider, ICrossChainLimitAppService crossChainLimitAppService)
    {
        _indexerCrossChainLimitInfoService = indexerCrossChainLimitInfoService;
        _evmTokensOptions = evmTokensOptions;
        _tokenAppService = tokenAppService;
        _chainAppService = chainAppService;
        _crossChainLimitsOptions = crossChainLimitsOptions;
        _tokenSymbolMappingProvider = tokenSymbolMappingProvider;
        _crossChainLimitAppService = crossChainLimitAppService;
    }

    public async Task<ListResultDto<CrossChainDailyLimitsDto>> GetCrossChainDailyLimitsAsync()
    {
        var crossChainLimits = _crossChainLimitsOptions.CurrentValue;
        Log.ForContext("chainId", crossChainLimits.ChainIdInfo.TokenFirstChainId).Information(
            "Get cross chain limit from settings.{chainId}", crossChainLimits.ChainIdInfo.TokenFirstChainId);
        var toChainIds = new HashSet<string>(crossChainLimits.ChainIdInfo.ToChainIds);
        var indexerCrossChainLimitInfos =
            await _indexerCrossChainLimitInfoService.GetAllCrossChainLimitInfoIndexAsync();
        //sort by config fromChainId first.
        var crossChainLimitInfos = indexerCrossChainLimitInfos
            .Where(item => toChainIds.Contains(item.ToChainId))
            .OrderByDescending(item => item.FromChainId == crossChainLimits.ChainIdInfo.TokenFirstChainId)
            .ToList();
        var fromChainDailyLimits = new Dictionary<string, FromChainDailyLimitsDto>();
        var tokenDict = new Dictionary<string, TokenDto>();
        foreach (var info in crossChainLimitInfos)
        {
            var key = info.Symbol;
            // skip if the current item does not match the fromChainId
            if (fromChainDailyLimits.TryGetValue(key, out var chainDailyLimitsDto)
                && chainDailyLimitsDto.FromChainId != info.FromChainId)
            {
                continue;
            }

            //avoid repeated get
            var tokenKey = info.ToChainId + "_" + info.Symbol;
            if (!tokenDict.TryGetValue(tokenKey, out var token))
            {
                token = await GetTokenInfoAsync(info.ToChainId, info.Symbol);
                if (token == null)
                {
                    continue;
                }

                tokenDict[tokenKey] = token;
            }

            var allowance = info.DefaultDailyLimit / (decimal)Math.Pow(10, token.Decimals);
            chainDailyLimitsDto ??= new FromChainDailyLimitsDto(info.FromChainId, info.Symbol, 0);
            chainDailyLimitsDto.Allowance += allowance;
            fromChainDailyLimits[key] = chainDailyLimitsDto;
        }

        //add token sort logic
        var resultList = fromChainDailyLimits.Values
            .Select(item => new CrossChainDailyLimitsDto(item.Token, item.Allowance))
            .OrderBy(item => crossChainLimits.GetTokenSortWeight(item.Token))
            .ToList();
        return new ListResultDto<CrossChainDailyLimitsDto>
        {
            Items = resultList
        };
    }

    public async Task<ListResultDto<CrossChainRateLimitsDto>> GetCrossChainRateLimitsAsync()
    {
        var result = new List<CrossChainRateLimitsDto>();
        var crossChainLimitInfos = await GetCrossChainLimitInfosAsync();
        var otherChainLimitInfos = await GetRateLimitsAsync();
        foreach (var crossChainLimitInfo in crossChainLimitInfos)
        {
            var chain = await _chainAppService.GetAsync(crossChainLimitInfo.Key.FromChainId);
            if (chain == null)
            {
                continue;
            }

            if (chain.Type == BlockchainType.AElf)
            {
                Log.ForContext("fromChainId", crossChainLimitInfo.Key.FromChainId)
                    .ForContext("toChainId", crossChainLimitInfo.Key.ToChainId).Information(
                        "Limit data processing，From chain:{fromChainId}, to chain:{toChainId}",
                        crossChainLimitInfo.Key.FromChainId, crossChainLimitInfo.Key.ToChainId);
                var receiptRateLimits =
                    await OfRateLimitInfos(crossChainLimitInfo.Value, crossChainLimitInfo.Key.FromChainId);
                var swapRateLimits = new List<RateLimitInfo>();
                if (otherChainLimitInfos.TryGetValue(crossChainLimitInfo.Key, out var value))
                {
                    swapRateLimits = OfEvmRateLimitInfos(value);
                }

                var chainDto = await _chainAppService.GetAsync(crossChainLimitInfo.Key.FromChainId);
                if (chainDto == null)
                {
                    continue;
                }

                var aelfChainId = chainDto.AElfChainId;
                crossChainLimitInfo.Key.FromChainId = ChainHelper.ConvertChainIdToBase58(aelfChainId);
                result.Add(new CrossChainRateLimitsDto
                {
                    FromChain = crossChainLimitInfo.Key.FromChainId,
                    ToChain = crossChainLimitInfo.Key.ToChainId,
                    ReceiptRateLimitsInfo = receiptRateLimits,
                    SwapRateLimitsInfo = swapRateLimits
                });
            }
            else if (chain.Type == BlockchainType.Evm)
            {
                Log.ForContext("fromChainId", crossChainLimitInfo.Key.FromChainId)
                    .ForContext("toChainId", crossChainLimitInfo.Key.ToChainId)
                    .Debug(
                        "Limit data processing，From chain:{fromChainId}, to chain:{toChainId}",
                        crossChainLimitInfo.Key.FromChainId, crossChainLimitInfo.Key.ToChainId);
                var swapRateLimits =
                    await OfRateLimitInfos(crossChainLimitInfo.Value, crossChainLimitInfo.Key.ToChainId);

                var receiptRateLimits = new List<RateLimitInfo>();
                if (otherChainLimitInfos.TryGetValue(crossChainLimitInfo.Key, out var value))
                {
                    receiptRateLimits = OfEvmRateLimitInfos(value);
                }

                var chainDto = await _chainAppService.GetAsync(crossChainLimitInfo.Key.ToChainId);
                if (chainDto == null)
                {
                    continue;
                }

                var aelfChainId = chainDto.AElfChainId;
                crossChainLimitInfo.Key.ToChainId = ChainHelper.ConvertChainIdToBase58(aelfChainId);
                result.Add(new CrossChainRateLimitsDto
                {
                    FromChain = crossChainLimitInfo.Key.FromChainId,
                    ToChain = crossChainLimitInfo.Key.ToChainId,
                    ReceiptRateLimitsInfo = receiptRateLimits,
                    SwapRateLimitsInfo = swapRateLimits
                });
            }
            else if (chain.Type == BlockchainType.Tvm)
            {
                Log.ForContext("fromChainId", crossChainLimitInfo.Key.FromChainId)
                    .ForContext("toChainId", crossChainLimitInfo.Key.ToChainId)
                    .Debug(
                        "Limit data processing，From chain:{fromChainId}, to chain:{toChainId}",
                        crossChainLimitInfo.Key.FromChainId, crossChainLimitInfo.Key.ToChainId);
                var swapRateLimits =
                    await OfRateLimitInfos(crossChainLimitInfo.Value, crossChainLimitInfo.Key.ToChainId);

                var receiptRateLimits = new List<RateLimitInfo>();
                if (otherChainLimitInfos.TryGetValue(crossChainLimitInfo.Key, out var value))
                {
                    receiptRateLimits = OfEvmRateLimitInfos(value);
                }

                var chainDto = await _chainAppService.GetAsync(crossChainLimitInfo.Key.ToChainId);
                if (chainDto == null)
                {
                    continue;
                }

                var aelfChainId = chainDto.AElfChainId;
                crossChainLimitInfo.Key.ToChainId = ChainHelper.ConvertChainIdToBase58(aelfChainId);
                result.Add(new CrossChainRateLimitsDto
                {
                    FromChain = crossChainLimitInfo.Key.FromChainId,
                    ToChain = crossChainLimitInfo.Key.ToChainId,
                    ReceiptRateLimitsInfo = receiptRateLimits,
                    SwapRateLimitsInfo = swapRateLimits
                });
            }
        }

        return new ListResultDto<CrossChainRateLimitsDto>
        {
            Items = result
        };
    }

    private async Task<Dictionary<CrossChainLimitKey, Dictionary<string, IndexerCrossChainLimitInfo>>>
        GetCrossChainLimitInfosAsync()
    {
        var crossChainLimits = _crossChainLimitsOptions.CurrentValue;
        var crossChainLimitInfoDictionary =
            new Dictionary<CrossChainLimitKey, Dictionary<string, IndexerCrossChainLimitInfo>>();
        var indexerCrossChainLimitInfos =
            await _indexerCrossChainLimitInfoService.GetAllCrossChainLimitInfoIndexAsync();
        var crossChainLimitInfos = indexerCrossChainLimitInfos
            .OrderBy(item => crossChainLimits.GetChainSortWeight(item.FromChainId, item.ToChainId))
            .ThenBy(item => crossChainLimits.GetTokenSortWeight(item.Symbol))
            .ToList();
        foreach (var item in crossChainLimitInfos)
        {
            Log.ForContext("fromChainId", item.FromChainId)
                .ForContext("toChainId", item.ToChainId)
                .Debug(
                    "Start to get limit info. From chain:{fromChainId}, to chain:{toChainId}, symbol:{symbol}",
                    item.FromChainId, item.ToChainId, item.Symbol);
            if (item.LimitType == CrossChainLimitType.Receipt)
            {
                var chain = await _chainAppService.GetByAElfChainIdAsync(
                    ChainHelper.ConvertBase58ToChainId(item.FromChainId));
                item.FromChainId = chain.Id;
            }
            else
            {
                var chain = await _chainAppService.GetByAElfChainIdAsync(
                    ChainHelper.ConvertBase58ToChainId(item.ToChainId));
                item.ToChainId = chain.Id;
            }

            var key = new CrossChainLimitKey
            {
                FromChainId = item.FromChainId,
                ToChainId = item.ToChainId
            };
            if (!crossChainLimitInfoDictionary.TryGetValue(key, out var tokenDictionary))
            {
                tokenDictionary = new Dictionary<string, IndexerCrossChainLimitInfo>();
                crossChainLimitInfoDictionary[key] = tokenDictionary;
            }

            tokenDictionary[item.Symbol] = item;
        }

        return crossChainLimitInfoDictionary;
    }

    private async Task<List<RateLimitInfo>> OfRateLimitInfos(
        Dictionary<string, IndexerCrossChainLimitInfo> tokenDictionary, string chainId)
    {
        var result = new List<RateLimitInfo>();
        foreach (var pair in tokenDictionary)
        {
            var token = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Symbol = pair.Key
            });
            var capacity = (decimal)pair.Value.Capacity;
            var refillRate = (decimal)pair.Value.RefillRate;
            var time = 0;
            if (capacity != 0 && refillRate != 0)
            {
                capacity = capacity / (decimal)Math.Pow(10, token.Decimals);
                refillRate = refillRate / (decimal)Math.Pow(10, token.Decimals);
                time = (int)Math.Ceiling(capacity / refillRate / CrossChainServerConsts.DefaultRateLimitSeconds);
            }

            result.Add(new RateLimitInfo
            {
                Token = pair.Key,
                Capacity = capacity,
                RefillRate = refillRate,
                MaximumTimeConsumed = time
            });
        }

        return result;
    }

    private static List<RateLimitInfo> OfEvmRateLimitInfos(
        Dictionary<string, TokenBucketDto> tokenDictionary)
    {
        return tokenDictionary.Select(pair => new RateLimitInfo
        {
            Token = pair.Key,
            Capacity = pair.Value.Capacity,
            RefillRate = pair.Value.RefillRate,
            MaximumTimeConsumed = pair.Value.MaximumTimeConsumed
        }).ToList();
    }

    private async Task<Dictionary<CrossChainLimitKey, Dictionary<string, TokenBucketDto>>> GetRateLimitsAsync()
    {
        var result = new Dictionary<CrossChainLimitKey, Dictionary<string, TokenBucketDto>>();
        var limits = await _crossChainLimitAppService.GetCrossChainRateLimitsAsync();
        foreach (var limit in limits)
        {
            var limitKey = new CrossChainLimitKey();
            var chain = await _chainAppService.GetByAElfChainIdAsync(
                ChainHelper.ConvertBase58ToChainId(limit.TargetChainId));
            if (limit.Type == CrossChainLimitType.Receipt)
            {
                limitKey.FromChainId = limit.ChainId;
                limitKey.ToChainId = chain.Id;
            }
            else
            {

                limitKey.FromChainId = chain.Id;
                limitKey.ToChainId = limit.ChainId;
            }

            if (!result.TryGetValue(limitKey, out var value))
            {
                value = new Dictionary<string, TokenBucketDto>();
            }

            var symbol =
                _tokenSymbolMappingProvider.GetMappingSymbol(limit.ChainId, limit.TargetChainId, limit.Token.Symbol);

            var tokenBucket = new TokenBucketDto();
            if (limit.Capacity != 0 || limit.Rate != 0)
            {
                tokenBucket.Capacity = limit.Capacity;
                tokenBucket.RefillRate = limit.Rate;
                tokenBucket.MaximumTimeConsumed =
                    (int)Math.Ceiling(tokenBucket.Capacity / tokenBucket.RefillRate /
                                      CrossChainServerConsts.DefaultRateLimitSeconds);
            }

            value[symbol] = tokenBucket;

            result[limitKey] = value;
        }

        return result;
    }
    
    [ExceptionHandler(typeof(Exception), Message = "Get token info failed.",
        ReturnDefault = ReturnDefault.Default, LogTargets = new[] { "chainId", "symbol" })]
    public virtual async Task<TokenDto> GetTokenInfoAsync(string chainId, string symbol)
    {
        var chain = await _chainAppService.GetByAElfChainIdAsync(
            ChainHelper.ConvertBase58ToChainId(chainId));
        var convertedChainId = chain.Id;

        return await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = convertedChainId,
            Symbol = symbol
        });
    }
}

public class FromChainDailyLimitsDto
{
    public string FromChainId { get; set; }
    public string Token { get; set; }
    public decimal Allowance { get; set; }

    public FromChainDailyLimitsDto(string fromChainId, string token, decimal allowance)
    {
        FromChainId = fromChainId;
        Token = token;
        Allowance = allowance;
    }
}

public class CrossChainLimitKey
{
    public string FromChainId { get; set; }

    public string ToChainId { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is not CrossChainLimitKey p)
        {
            return false;
        }

        return FromChainId == p.FromChainId && ToChainId == p.ToChainId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FromChainId, ToChainId);
    }
}