using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.EvmIndexer.Dtos;
using AElf.CrossChainServer.EvmIndexer.Dtos.Event;
using AElf.CrossChainServer.EvmIndexer.Dtos.Event.Bridge;
using AElf.CrossChainServer.EvmIndexer.Dtos.MessageDto;
using AElf.CrossChainServer.Tokens;
using AElf.Types;
using Nethereum.BlockchainProcessing.BlockStorage.Entities.Mapping;
using Nethereum.Contracts;
using Nethereum.Util;
using Newtonsoft.Json;
using Serilog;

namespace AElf.CrossChainServer.EvmIndexer.Processor.Bridge;

public class NewReceiptEventProcessor(
    ITokenAppService tokenAppService,
    ICrossChainTransferAppService crossChainTransferAppService) : IEvmEventProcessor<NewReceiptEvent>
{
    public async Task HandleAsync(string chainId, EventLog<NewReceiptEvent> eventLog)
    {
        var message = GenerateNewReceiptReceivedMessage(eventLog);
        Log.Debug("[NewReceiptEventProcessor] Received Event NewReceipt --> {EventData}",
            JsonConvert.SerializeObject(message));
        var token = await tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = message.Asset
        });
        await crossChainTransferAppService.TransferAsync(new CrossChainTransferInput
        {
            TransferTransactionId = message.TransactionId,
            FromAddress = message.Owner,
            ReceiptId = message.ReceiptId[2..],
            ToAddress = Address.FromBytes(message.TargetAddress).ToBase58(),
            TransferAmount = (decimal)((BigDecimal)message.Amount / BigInteger.Pow(10, token.Decimals)),
            TransferTime = DateTimeHelper.FromUnixTimeMilliseconds((long)message.BlockTime * 1000),
            FromChainId = chainId,
            ToChainId = message.TargetChainId,
            TransferBlockHeight = message.BlockNumber,
            TransferTokenId = token.Id,
            TransferStatus = ReceiptStatus.Pending
        });
    }

    private NewReceiptReceivedMessageDto GenerateNewReceiptReceivedMessage(EventLog<NewReceiptEvent> eventData)
    {
        return new NewReceiptReceivedMessageDto
        {
            TransactionId = eventData.Log.TransactionHash,
            BlockHash = eventData.Log.BlockHash,
            BlockNumber = eventData.Log.BlockNumber.ToLong(),
            Owner = eventData.Event.Owner,
            Asset = eventData.Event.Asset,
            Amount = eventData.Event.Amount,
            TargetChainId = eventData.Event.TargetChainId,
            TargetAddress = eventData.Event.TargetAddress,
            ReceiptId = eventData.Event.ReceiptId,
            BlockTime = eventData.Event.BlockTime
        };
    }
}