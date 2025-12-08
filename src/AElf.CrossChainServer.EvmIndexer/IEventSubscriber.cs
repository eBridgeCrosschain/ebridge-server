using System;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using Serilog;

namespace AElf.CrossChainServer.EvmIndexer;

public interface IEventSubscriber
{
    Task SubscribeEventAsync<TEventDto>(string chainId, StreamingWebSocketClient client,
        Action<EventLog<TEventDto>> onEventDecoded,
        string contractAddress) where TEventDto : IEventDTO, new();
}

public class EventSubscriber
    : IEventSubscriber
{
    public async Task SubscribeEventAsync<TEventDto>(string chainId, StreamingWebSocketClient client,
        Action<EventLog<TEventDto>> onEventDecoded, string contractAddress)
        where TEventDto : IEventDTO, new()
    {
        Log.Debug("[EventProcessorBase] Starting subscription on Network. ChainId:{ChainId}",chainId);
        var filter = Event<TEventDto>.GetEventABI().CreateFilterInput(contractAddress);

        var eventSubscription = new EthLogsObservableSubscription(client);
        eventSubscription.GetSubscriptionDataResponsesAsObservable().Subscribe(log =>
            {
                Log.Debug(
                    $"[EventProcessorBase] Block: {log.BlockHash}, BlockHeight: {log.BlockNumber}");
                try
                {
                    var decoded = Event<TEventDto>.DecodeEvent(log);
                    if (decoded == null)
                    {
                        Log.Warning(
                            $"[EventProcessorBase]DecodeEvent failed at BlockHeight: {log.BlockNumber}");
                        return;
                    }

                    onEventDecoded(decoded);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"[EventProcessorBase] Failed to decode event.");
                }
            },
            exception =>
                Log.Error(
                    $"[EventProcessorBase]ChainId:{chainId}. Subscription error: {exception.Message}")
        );
        await eventSubscription.SubscribeAsync(filter);
        Log.Debug($"[EventProcessorBase]ChainId:{chainId}. Subscription successful for {typeof(TEventDto).Name}");
    }

}