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

public class EvmDailyLimitSetSyncProvider(
    ISettingManager settingManager,
    IOptionsSnapshot<EvmContractSyncOptions> evmContractSyncOptions,
    ICrossChainLimitAppService crossChainLimitAppService,
    ITokenAppService tokenAppService,
    IOptionsSnapshot<TokenLimitSwapInfoOptions> tokenSwapInfoOptions) : EvmIndexerSyncProviderBase(settingManager)
{
    private readonly TokenLimitSwapInfoOptions _tokenLimitSwapInfoOptions = tokenSwapInfoOptions.Value;

    private readonly EvmContractSyncOptions _evmContractSyncOptions = evmContractSyncOptions.Value;
    private const string DailyLimitSetEvent = "DailyLimitSet(bytes32,uint32,uint256,uint256)";
    private static string DailyLimitSetEventSignature => DailyLimitSetEvent.GetEventSignature();

    protected override string SyncType { get; } = CrossChainServerSettings.EvmDailyLimitSetIndexerSync;

    protected override async Task<long> HandleDataAsync(string chainId, long startHeight, long endHeight)
    {
        Log.ForContext("chainId", chainId).Debug(
            "Start to sync token limit info {chainId} from {StartHeight} to {EndHeight}", chainId, startHeight,
            endHeight);
        var limiterContract = _evmContractSyncOptions.IndexerInfos[chainId].LimiterContract;
        var filterLogsAndEventsDto = await GetContractLogsAndParseAsync<DailyLimitSetEvent>(chainId, limiterContract,
            startHeight,
            endHeight, DailyLimitSetEventSignature);
        if (filterLogsAndEventsDto?.Events == null || filterLogsAndEventsDto.Events?.Count == 0)
        {
            return endHeight;
        }

        await ProcessLogEventAsync(chainId, filterLogsAndEventsDto);

        return filterLogsAndEventsDto.Events.Max(e => e.Log.BlockNumber);
    }

    private async Task ProcessLogEventAsync(string chainId,
        FilterLogsAndEventsDto<DailyLimitSetEvent> filterLogsAndEventsDto)
    {
        Log.Debug("Sync daily limit set log event, chainId: {chainId}", chainId);

        foreach (var dailyLimitSet in filterLogsAndEventsDto.Events)
        {
            var log = dailyLimitSet.Log;
            var events = dailyLimitSet.Event;
            Log.Debug("Sync daily limit set log event, chainId: {chainId}, log: {log}", chainId,
                JsonSerializer.Serialize(events)
            );
            var swapInfo = _tokenLimitSwapInfoOptions.SwapTokenInfos[events.DailyLimitId.ToHex()];
            if (swapInfo == null)
            {
                Log.Error("DailyLimitSetEventProcessor: SwapInfo not found for id: {Id}", events.DailyLimitId.ToHex());
                return;
            }

            var token = await tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Address = swapInfo.TokenAddress
            });
            var dailyLimitAmount = (decimal)((BigDecimal)events.DailyLimit / BigInteger.Pow(10, token.Decimals));
            var remainTokenAmount =
                (decimal)((BigDecimal)events.RemainTokenAmount / BigInteger.Pow(10, token.Decimals));

            await crossChainLimitAppService.SetCrossChainDailyLimitAsync(new SetCrossChainDailyLimitInput
            {
                ChainId = swapInfo.FromChainId,
                Type = swapInfo.LimitType == 0
                    ? CrossChainLimitType.Receipt
                    : CrossChainLimitType.Swap,
                DailyLimit = dailyLimitAmount,
                RefreshTime = events.RefreshTime,
                RemainAmount = remainTokenAmount,
                TokenId = token.Id,
                TargetChainId = swapInfo.ToChainId,
            });
        }
    }
}