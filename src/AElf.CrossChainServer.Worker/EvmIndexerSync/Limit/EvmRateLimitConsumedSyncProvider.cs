using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.Tokens;
using AElf.CrossChainServer.Worker.EvmIndexerSync.Dtos.Limit;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Serilog;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync.Limit;

public class EvmRateLimitConsumedSyncProvider(
    ISettingManager settingManager,
    IOptionsSnapshot<EvmContractSyncOptions> evmContractSyncOptions,
    ICrossChainLimitAppService crossChainLimitAppService,
    ITokenAppService tokenAppService,IOptionsSnapshot<TokenLimitSwapInfoOptions> tokenSwapInfoOptions) : EvmIndexerSyncProviderBase(settingManager)
{
    private readonly TokenLimitSwapInfoOptions _tokenLimitSwapInfoOptions = tokenSwapInfoOptions.Value;

    private readonly EvmContractSyncOptions _evmContractSyncOptions = evmContractSyncOptions.Value;
    private const string RateLimitConsumedEvent = "TokenRateLimitConsumed(bytes32,address,uint256)";
    private static string RateLimitConsumedEventSignature => RateLimitConsumedEvent.GetEventSignature();

    protected override string SyncType { get; } = CrossChainServerSettings.EvmRateLimitConsumedIndexerSync;

    protected override async Task<long> HandleDataAsync(string chainId, long startHeight, long endHeight)
    {
        Log.ForContext("chainId", chainId).Debug(
            "Start to sync rate limit consumed info {chainId} from {StartHeight} to {EndHeight}", chainId, startHeight,
            endHeight);
        var limiterContract = _evmContractSyncOptions.IndexerInfos[chainId].LimiterContract;
        var filterLogsAndEventsDto = await GetContractLogsAndParseAsync<TokenRateLimitConsumedEvent>(chainId, limiterContract,
            startHeight,
            endHeight, RateLimitConsumedEventSignature);
        if (filterLogsAndEventsDto?.Events == null || filterLogsAndEventsDto.Events?.Count == 0)
        {
            return endHeight;
        }

        await ProcessLogEventAsync(chainId, filterLogsAndEventsDto);

        return filterLogsAndEventsDto.Events.Max(e => e.Log.BlockNumber);

    }
    
    private async Task ProcessLogEventAsync(string chainId,
        FilterLogsAndEventsDto<TokenRateLimitConsumedEvent> filterLogsAndEventsDto)
    {
        Log.Debug("Sync rate limit consumed log event, chainId: {chainId}", chainId);

        foreach (var dailyLimitSet in filterLogsAndEventsDto.Events)
        {
            var log = dailyLimitSet.Log;
            var events = dailyLimitSet.Event;
            Log.Debug("Sync rate limit consumed log event, chainId: {chainId}, log: {log}", chainId,
                JsonSerializer.Serialize(events)
            );
            var swapInfo = _tokenLimitSwapInfoOptions.SwapTokenInfos[events.BucketId.ToHex()];
            if (swapInfo == null)
            {
                Log.Error("TokenDailyLimitConsumedEventProcessor: SwapInfo not found for id: {Id}", events.BucketId.ToHex());
                return;
            }
            var token = await tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Address = swapInfo.TokenAddress
            });
            var consumeAmount = (decimal)((BigDecimal)events.Amount / BigInteger.Pow(10, token.Decimals));
            await crossChainLimitAppService.ConsumeCrossChainRateLimitAsync(new ConsumeCrossChainRateLimitInput
            {
                ChainId = swapInfo.FromChainId,
                Type = swapInfo.LimitType == 0
                    ? CrossChainLimitType.Receipt
                    : CrossChainLimitType.Swap,
                TargetChainId = swapInfo.ToChainId,
                TokenId = token.Id,
                Amount = consumeAmount
            });
        }
    }
    
}