using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.Tokens;
using AElf.CrossChainServer.Worker.EvmIndexerSync.Dtos;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Serilog;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public class EvmTokenSwapSyncProvider(
    ISettingManager settingManager,
    IOptionsSnapshot<EvmContractSyncOptions> evmContractSyncOptions,
    ICrossChainTransferAppService crossChainTransferAppService,
    ITokenAppService tokenAppService) : EvmIndexerSyncProviderBase(settingManager)
{
    private readonly EvmContractSyncOptions _evmContractSyncOptions = evmContractSyncOptions.Value;

    private const string TokenSwappedEvent = "TokenSwapEvent(address,address,uint256,string,string,uint256)";
    private static string TokenSwappedEventSignature => TokenSwappedEvent.GetEventSignature();

    protected override string SyncType { get; } = CrossChainServerSettings.EvmTokenSwappedIndexerSync;

    protected override async Task<long> HandleDataAsync(string chainId, long startHeight, long endHeight)
    {
        Log.ForContext("chainId", chainId).Debug(
            "Start to sync token swap info {chainId} from {StartHeight} to {EndHeight}",
            chainId, startHeight, endHeight);
        var bridgeOutContract = _evmContractSyncOptions.ContractAddresses[chainId].BridgeOutContract;
        var filterLogsAndEventsDto = await GetContractLogsAndParseAsync<TokenSwappedEvent>(chainId, bridgeOutContract,
            startHeight,
            endHeight, TokenSwappedEventSignature);
        if (filterLogsAndEventsDto?.Events == null || filterLogsAndEventsDto.Events?.Count == 0)
        {
            return endHeight;
        }

        await ProcessLogEventAsync(chainId, filterLogsAndEventsDto);

        return filterLogsAndEventsDto.Events.Max(e => e.Log.BlockNumber);
    }

    private async Task ProcessLogEventAsync(string chainId,
        FilterLogsAndEventsDto<TokenSwappedEvent> filterLogsAndEventsDto)
    {
        Log.Debug("Sync token swap log event, chainId: {chainId}", chainId);

        foreach (var tokenSwapped in filterLogsAndEventsDto.Events)
        {
            var log = tokenSwapped.Log;
            var events = tokenSwapped.Event;
            Log.Debug("Sync token swap log event, chainId: {chainId}, log: {log}", chainId,
                JsonSerializer.Serialize(events)
            );
            var token = await tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Address = tokenSwapped.Event.Token
            });
            await crossChainTransferAppService.ReceiveAsync(new CrossChainReceiveInput
            {
                ReceiveTransactionId = log.TransactionHash,
                ReceiptId = events.ReceiptId,
                ToAddress = events.ReceiveAddress,
                FromChainId = events.FromChainId,
                ToChainId = chainId,
                ReceiveAmount = (decimal)((BigDecimal)events.Amount / BigInteger.Pow(10, token.Decimals)),
                ReceiveTime = DateTimeHelper.FromUnixTimeMilliseconds((long)events.BlockTime * 1000),
                ReceiveTokenId = token.Id,
            });
        }
    }
}