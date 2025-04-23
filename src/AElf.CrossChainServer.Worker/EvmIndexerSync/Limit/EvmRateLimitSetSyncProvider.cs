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

public class EvmRateLimitSetSyncProvider(
    ISettingManager settingManager,
    IOptionsSnapshot<EvmContractSyncOptions> evmContractSyncOptions,
    ICrossChainLimitAppService crossChainLimitAppService,
    ITokenAppService tokenAppService,
    IOptionsSnapshot<TokenLimitSwapInfoOptions> tokenSwapInfoOptions) : EvmIndexerSyncProviderBase(settingManager)
{
    private readonly TokenLimitSwapInfoOptions _tokenLimitSwapInfoOptions = tokenSwapInfoOptions.Value;

    private readonly EvmContractSyncOptions _evmContractSyncOptions = evmContractSyncOptions.Value;
    private const string RateLimitSetEvent = "ConfigChanged(bytes32,bool,uint128,uint128,uint128)";

    private static string RateLimitSetEventSignature => RateLimitSetEvent.GetEventSignature();

    protected override string SyncType { get; } = CrossChainServerSettings.EvmRateLimitSetIndexerSync;

    protected override async Task<long> HandleDataAsync(string chainId, long startHeight, long endHeight)
    {
        Log.ForContext("chainId", chainId).Debug(
            "Start to sync token rate limit set info {chainId} from {StartHeight} to {EndHeight}", chainId, startHeight,
            endHeight);
        var limiterContract = _evmContractSyncOptions.IndexerInfos[chainId].LimiterContract;
        var filterLogsAndEventsDto = await GetContractLogsAndParseAsync<ConfigChangedEvent>(chainId, limiterContract,
            startHeight,
            endHeight, RateLimitSetEventSignature);
        if (filterLogsAndEventsDto?.Events == null || filterLogsAndEventsDto.Events?.Count == 0)
        {
            return endHeight;
        }

        await ProcessLogEventAsync(chainId, filterLogsAndEventsDto);

        return filterLogsAndEventsDto.Events.Max(e => e.Log.BlockNumber);
    }

    private async Task ProcessLogEventAsync(string chainId,
        FilterLogsAndEventsDto<ConfigChangedEvent> filterLogsAndEventsDto)
    {
        Log.Debug("Sync rate limit set log event, chainId: {chainId}", chainId);

        foreach (var dailyLimitSet in filterLogsAndEventsDto.Events)
        {
            var log = dailyLimitSet.Log;
            var events = dailyLimitSet.Event;
            Log.Debug("Sync rate limit set log event, chainId: {chainId}, transaction:{tx}, log: {log}", chainId,
                JsonSerializer.Serialize(log), JsonSerializer.Serialize(events)
            );
            var swapInfo = _tokenLimitSwapInfoOptions.SwapTokenInfos[events.BucketId.ToHex()];
            if (swapInfo == null)
            {
                Log.Error("ConfigChangedEventProcessor: SwapInfo not found for id: {Id}", events.BucketId.ToHex());
                return;
            }

            var token = await tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Address = swapInfo.TokenAddress
            });
            var capacityAmount = (decimal)((BigDecimal)events.TokenCapacity / BigInteger.Pow(10, token.Decimals));
            var remainTokenAmount =
                (decimal)((BigDecimal)events.CurrentTokenAmount / BigInteger.Pow(10, token.Decimals));
            var rate = (decimal)((BigDecimal)events.Rate / BigInteger.Pow(10, token.Decimals));

            await crossChainLimitAppService.SetCrossChainRateLimitAsync(new SetCrossChainRateLimitInput()
            {
                ChainId = swapInfo.FromChainId,
                Type = swapInfo.LimitType == 0
                    ? CrossChainLimitType.Receipt
                    : CrossChainLimitType.Swap,
                TokenId = token.Id,
                TargetChainId = swapInfo.ToChainId,
                Rate = rate,
                CurrentAmount = remainTokenAmount,
                Capacity = capacityAmount,
                IsEnable = events.IsEnabled
            });
        }
    }
}