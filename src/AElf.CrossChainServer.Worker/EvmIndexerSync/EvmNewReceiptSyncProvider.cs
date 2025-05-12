using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.Tokens;
using AElf.CrossChainServer.Worker.EvmIndexerSync.Dtos;
using AElf.Types;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Serilog;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public class EvmNewReceiptSyncProvider(
    ISettingManager settingManager,
    IOptionsSnapshot<EvmContractSyncOptions> evmContractSyncOptions,
    ICrossChainTransferAppService crossChainTransferAppService,
    ITokenAppService tokenAppService) : EvmIndexerSyncProviderBase(settingManager)
{
    private readonly EvmContractSyncOptions _evmContractSyncOptions = evmContractSyncOptions.Value;

    private const string NewReceiptEvent = "NewReceipt(string,address,address,uint256,string,bytes32,uint256)";
    private static string NewReceiptEventSignature => NewReceiptEvent.GetEventSignature();

    protected override string SyncType { get; } = CrossChainServerSettings.EvmNewReceiptIndexerSync;

    protected override async Task<long> HandleDataAsync(string chainId, long startHeight, long endHeight)
    {
        Log.ForContext("chainId", chainId).Debug(
            "Start to sync new receipt info {chainId} from {StartHeight} to {EndHeight}",
            chainId, startHeight, endHeight);
        var bridgeInContract = _evmContractSyncOptions.ContractAddresses[chainId].BridgeInContract;
        var filterLogsAndEventsDto = await GetContractLogsAndParseAsync<NewReceiptEvent>(chainId, bridgeInContract,
            startHeight,
            endHeight, NewReceiptEventSignature);
        if (filterLogsAndEventsDto?.Events == null || filterLogsAndEventsDto.Events?.Count == 0)
        {
            return endHeight;
        }

        await ProcessLogEventAsync(chainId, filterLogsAndEventsDto);

        return filterLogsAndEventsDto.Events.Max(e => e.Log.BlockNumber);
    }

    private async Task ProcessLogEventAsync(string chainId,
        FilterLogsAndEventsDto<NewReceiptEvent> filterLogsAndEventsDto)
    {
        Log.Debug("Sync new receipt log event, chainId: {chainId}", chainId);

        foreach (var newReceipt in filterLogsAndEventsDto.Events)
        {
            var log = newReceipt.Log;
            var events = newReceipt.Event;
            Log.Debug("Sync new receipt log event, chainId: {chainId}, log: {log}", chainId,
                JsonSerializer.Serialize(events));
            var token = await tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Address = events.Asset
            });
            await crossChainTransferAppService.TransferAsync(new CrossChainTransferInput
            {
                TransferTransactionId = log.TransactionHash,
                FromAddress = events.Owner,
                ReceiptId = events.ReceiptId[2..],
                ToAddress = Address.FromBytes(events.TargetAddress).ToBase58(),
                TransferAmount = (decimal)((BigDecimal)events.Amount / BigInteger.Pow(10, token.Decimals)),
                TransferTime = DateTimeHelper.FromUnixTimeMilliseconds((long)events.BlockTime * 1000),
                FromChainId = chainId,
                ToChainId = events.TargetChainId,
                TransferBlockHeight = log.BlockNumber,
                TransferTokenId = token.Id
            });
        }
    }
}