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

public class EvmDailyLimitConsumedSyncProvider
(
    ISettingManager settingManager,
    IOptionsSnapshot<EvmContractSyncOptions> evmContractSyncOptions,
    ICrossChainLimitAppService crossChainLimitAppService,
    ITokenAppService tokenAppService,IOptionsSnapshot<TokenLimitSwapInfoOptions> tokenSwapInfoOptions) : EvmIndexerSyncProviderBase(settingManager)
{
    private readonly TokenLimitSwapInfoOptions _tokenLimitSwapInfoOptions = tokenSwapInfoOptions.Value;

    private readonly EvmContractSyncOptions _evmContractSyncOptions = evmContractSyncOptions.Value;
    
    private const string DailyLimitConsumedEvent = "TokenDailyLimitConsumed(bytes32,address,uint256)";
    
    private static string DailyLimitConsumedEventSignature => DailyLimitConsumedEvent.GetEventSignature();

    protected override string SyncType { get; } = CrossChainServerSettings.EvmDailyLimitConsumedIndexerSync;

    protected override async Task<long> HandleDataAsync(string chainId, long startHeight, long endHeight)
    {
        Log.ForContext("chainId", chainId).Debug(
            "Start to sync daily limit consumed info {chainId} from {StartHeight} to {EndHeight}", chainId, startHeight,
            endHeight);
        var limiterContract = _evmContractSyncOptions.IndexerInfos[chainId].LimiterContract;
        var filterLogsAndEventsDto = await GetContractLogsAndParseAsync<TokenDailyLimitConsumedEvent>(chainId, limiterContract,
            startHeight,
            endHeight, DailyLimitConsumedEventSignature);
        if (filterLogsAndEventsDto?.Events == null || filterLogsAndEventsDto.Events?.Count == 0)
        {
            return endHeight;
        }

        await ProcessLogEventAsync(chainId, filterLogsAndEventsDto);

        return filterLogsAndEventsDto.Events.Max(e => e.Log.BlockNumber);

    }
    
    private async Task ProcessLogEventAsync(string chainId,
        FilterLogsAndEventsDto<TokenDailyLimitConsumedEvent> filterLogsAndEventsDto)
    {
        Log.Debug("Sync daily limit consumed log event, chainId: {chainId}", chainId);

        foreach (var dailyLimitSet in filterLogsAndEventsDto.Events)
        {
            var log = dailyLimitSet.Log;
            var events = dailyLimitSet.Event;
            Log.Debug("Sync daily limit consumed log event, chainId: {chainId}, log: {log}", chainId,
                JsonSerializer.Serialize(events)
            );
            var swapInfo = _tokenLimitSwapInfoOptions.SwapTokenInfos[events.DailyLimitId.ToHex()];
            if (swapInfo == null)
            {
                Log.Error("TokenDailyLimitConsumedEventProcessor: SwapInfo not found for id: {Id}", events.DailyLimitId.ToHex());
                return;
            }
            var token = await tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Address = swapInfo.TokenAddress
            });
            var consumeAmount = (decimal)((BigDecimal)events.Amount / BigInteger.Pow(10, token.Decimals));
            await crossChainLimitAppService.ConsumeCrossChainDailyLimitAsync(new ConsumeCrossChainDailyLimitInput
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