using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nethereum.Util;
using Serilog;
using Volo.Abp.Domain.Entities;

namespace AElf.CrossChainServer.CrossChain;

public interface ICheckTransferProvider
{
    Task<bool> CheckTransferAsync(string fromChainId, string toChainId, Guid tokenId, decimal transferAmount);
    Task<bool> CheckTokenExistAsync(string fromChainId, string toChainId, Guid tokenId);
}

public class CheckTransferProvider : ICheckTransferProvider
{
    private readonly IIndexerCrossChainLimitInfoService _indexerCrossChainLimitInfoService;
    private readonly IChainAppService _chainAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly ITokenSymbolMappingProvider _tokenSymbolMappingProvider;


    public CheckTransferProvider(
        IIndexerCrossChainLimitInfoService indexerCrossChainLimitInfoService, IChainAppService chainAppService,
        ITokenAppService tokenAppService, ITokenSymbolMappingProvider tokenSymbolMappingProvider)
    {
        _indexerCrossChainLimitInfoService = indexerCrossChainLimitInfoService;
        _chainAppService = chainAppService;
        _tokenAppService = tokenAppService;
        _tokenSymbolMappingProvider = tokenSymbolMappingProvider;
    }

    public async Task<bool> CheckTransferAsync(string fromChainId, string toChainId, Guid tokenId,
        decimal transferAmount)
    {
        var (amount,symbol) = await GetTokenInfoAsync(fromChainId, toChainId, tokenId, transferAmount);
        Log.ForContext("fromChainId", fromChainId).ForContext("toChainId", toChainId).Debug(
        var (amount,symbol) = await GetTokenTransferAmountAsync(fromChainId, toChainId, tokenId, transferAmount);
        Logger.LogInformation(
            "Start to check limit. From chain:{fromChainId}, to chain:{toChainId}, token symbol:{symbol}, transfer amount:{amount}",
            fromChainId, toChainId, symbol, amount);
        var chain = await _chainAppService.GetAsync(toChainId);
        if (chain == null)
        {
            Log.Warning("No chain info.");
            return false;
        }
        toChainId = ChainHelper.ConvertChainIdToBase58(chain.AElfChainId);
        var limitInfo =
            (await _indexerCrossChainLimitInfoService.GetCrossChainLimitInfoIndexAsync(fromChainId, toChainId,
                symbol)).FirstOrDefault();
        if (limitInfo == null)
        {
            Log.Warning("No limit info.");
            return true;
        }
        var time = DateTime.UtcNow;
        if (time.Subtract(limitInfo.RefreshTime).TotalSeconds >= CrossChainServerConsts.DefaultDailyLimitRefreshTime)
        {
            Log.Debug("Daily limit refresh.");
            limitInfo.CurrentDailyLimit = limitInfo.DefaultDailyLimit;
        }
        if (limitInfo.Capacity == 0)
        {
            Log.Warning("Rate limit does not set.");
            return amount <= limitInfo.CurrentDailyLimit;
        }
        var timeDiff = time.Subtract(limitInfo.BucketUpdateTime).TotalSeconds;
        var rateLimit = Math.Min(limitInfo.Capacity,
            limitInfo.CurrentBucketTokenAmount + timeDiff * limitInfo.RefillRate);
        Log.Information(
            "Limit info,daily limit:{dailyLimit},capacity:{capacity},current bucket amount:{currentBucket},bucketUpdateTime:{bucketUpdateTime},rate:{rate},now:{timeNow},time diff:{dif},rate limit:{limit}.",
            limitInfo.CurrentDailyLimit, limitInfo.Capacity, limitInfo.CurrentBucketTokenAmount,
            limitInfo.BucketUpdateTime, limitInfo.RefillRate, time, timeDiff, rateLimit);

        return amount <= limitInfo.CurrentDailyLimit && amount <= (decimal)rateLimit;
    }

    // todo:exception
    private async Task<(decimal,string)> GetTokenTransferAmountAsync(string fromChainId, string toChainId, Guid tokenId,
        decimal transferAmount)
    {
        var token = await GetTokenInfoAsync(fromChainId, toChainId, tokenId);
        if (token == null)
        {
            throw new EntityNotFoundException("Token not exist.");
        }
        return (transferAmount * (decimal)Math.Pow(10, token.Decimals),token.Symbol);
    }

    private async Task<TokenDto> GetTokenInfoAsync(string fromChainId, string toChainId, Guid tokenId)
    {
        var transferToken = await _tokenAppService.GetAsync(tokenId);
        var symbol =
            _tokenSymbolMappingProvider.GetMappingSymbol(fromChainId, toChainId, transferToken.Symbol);
        try
        {
            var token = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = toChainId,
                Symbol = symbol
            });
            return token;
        }
        catch (Exception e)
        {
            Logger.LogError("Get token info error:{error}", e.Message);
            return null;
        }
    }
    public async Task<bool> CheckTokenExistAsync(string fromChainId, string toChainId, Guid tokenId)
    {
        var token = await GetTokenInfoAsync(fromChainId, toChainId, tokenId);
        return token != null;
    }
    

}