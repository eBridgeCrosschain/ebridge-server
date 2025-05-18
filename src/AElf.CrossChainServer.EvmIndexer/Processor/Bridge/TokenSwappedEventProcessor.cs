using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.EvmIndexer.Dtos;
using AElf.CrossChainServer.EvmIndexer.Dtos.Event;
using AElf.CrossChainServer.EvmIndexer.Dtos.Event.Bridge;
using AElf.CrossChainServer.EvmIndexer.Dtos.MessageDto;
using AElf.CrossChainServer.Tokens;
using Nethereum.BlockchainProcessing.BlockStorage.Entities.Mapping;
using Nethereum.Contracts;
using Nethereum.Util;
using Newtonsoft.Json;
using Serilog;

namespace AElf.CrossChainServer.EvmIndexer.Processor.Bridge;

public class TokenSwappedEventProcessor(
    ITokenAppService tokenAppService,
    ICrossChainTransferAppService crossChainTransferAppService) : IEvmEventProcessor<TokenSwappedEvent>
{
    public async Task HandleAsync(string chainId, EventLog<TokenSwappedEvent> eventLog)
    {
        var message = GenerateTokenSwappedMessage(eventLog);
        Log.Debug("[TokenSwappedEventProcessor] Received Event TokenSwapped --> {EventData}",
            JsonConvert.SerializeObject(message));
        var token = await tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = message.Token
        });
        await crossChainTransferAppService.ReceiveAsync(new CrossChainReceiveInput
        {
            ReceiveTransactionId = message.TransactionId,
            ReceiptId = message.ReceiptId,
            ToAddress = message.ReceiveAddress,
            FromChainId = message.FromChainId,
            ToChainId = chainId,
            ReceiveAmount = (decimal)((BigDecimal)message.Amount / BigInteger.Pow(10, token.Decimals)),
            ReceiveTime = DateTimeHelper.FromUnixTimeMilliseconds((long)message.BlockTime * 1000),
            ReceiveTokenId = token.Id,
            ReceiveStatus = ReceiptStatus.Pending,
            ReceiveBlockHeight = message.BlockNumber
        });
    }

    private TokenSwappedMessageDto GenerateTokenSwappedMessage(EventLog<TokenSwappedEvent> eventData)
    {
        return new TokenSwappedMessageDto
        {
            TransactionId = eventData.Log.TransactionHash,
            BlockNumber = eventData.Log.BlockNumber.ToLong(),
            ReceiveAddress = eventData.Event.ReceiveAddress,
            Token = eventData.Event.Token,
            Amount = eventData.Event.Amount,
            ReceiptId = eventData.Event.ReceiptId,
            FromChainId = eventData.Event.FromChainId,
            BlockTime = eventData.Event.BlockTime
        };
    }
}