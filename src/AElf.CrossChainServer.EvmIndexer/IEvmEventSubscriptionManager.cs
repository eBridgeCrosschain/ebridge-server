using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Serilog;

namespace AElf.CrossChainServer.EvmIndexer;

public interface IEvmEventSubscriptionManager
{
    Task SubscribeAsync(string chainId, IEnumerable<ContractEventSubscriptionInfo> subscriptions,
        StreamingWebSocketClient client, int pingDelay, CancellationToken cancellationToken = default);
}

public class EvmEventSubscriptionManager(
    IEventSubscriber eventSubscriber,
    IServiceProvider serviceProvider)
    : IEvmEventSubscriptionManager
{
    public async Task SubscribeAsync(string chainId, IEnumerable<ContractEventSubscriptionInfo> subscriptions,
        StreamingWebSocketClient client, int pingDelay, CancellationToken cancellationToken = default)
    {
        if (subscriptions == null)
        {
            Log.Warning("[EvmEventSubscriptionManager] No subscriptions provided for chainId: {ChainId}", chainId);
            return;
        }

        foreach (var info in subscriptions)
        {
            foreach (var eventType in info.EventTypes)
            {
                Log.Debug(
                    "[EvmEventSubscriptionManager] Subscribing to event {EventType} on contract {Contract} for chainId: {ChainId}",
                    eventType.Name, info.ContractAddress, chainId);

                var method = typeof(EvmEventSubscriptionManager)
                    .GetMethod(nameof(StartSubscriptionAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(eventType);

                await (Task)method.Invoke(this, [chainId, client, info.ContractAddress])!;
            }
        }
    }

    private async Task StartSubscriptionAsync<TEventDto>(
        string chainId,
        StreamingWebSocketClient client,
        string contractAddress)
        where TEventDto : IEventDTO, new()
    {
        var handler = serviceProvider.GetRequiredService<IEvmEventProcessor<TEventDto>>();

        await eventSubscriber.SubscribeEventAsync<TEventDto>(
            chainId,
            client,
            async eventLog => await handler.HandleAsync(chainId, eventLog),
            contractAddress);
    }
    // public async Task SubscribeAsync(string chainId, IEnumerable<ContractEventSubscriptionInfo> subscriptions,
    //     StreamingWebSocketClient client, int pingDelay, CancellationToken cancellationToken = default)
    // {
    //     if (subscriptions == null)
    //     {
    //         Log.Warning("[EvmEventSubscriptionManager] No subscriptions provided for chainId: {ChainId}", chainId);
    //         return;
    //     }
    //
    //     var subscriptionTasks = new List<Task>();
    //
    //     foreach (var info in subscriptions)
    //     {
    //         foreach (var eventType in info.EventTypes)
    //         {
    //             Log.Debug("[EvmEventSubscriptionManager] Subscribing to event {EventType} on contract {Contract} for chainId: {ChainId}",
    //                 eventType.Name, info.ContractAddress, chainId);
    //
    //             var method = typeof(EvmEventSubscriptionManager)
    //                 .GetMethod(nameof(StartSubscriptionWithRetryAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
    //                 .MakeGenericMethod(eventType);
    //
    //             var task = (Task)method.Invoke(this, [chainId, client, info.ContractAddress, cancellationToken])!;
    //             subscriptionTasks.Add(task);
    //         }
    //     }
    //
    //     await Task.WhenAll(subscriptionTasks);
    // }
    //
    // private Task StartSubscriptionWithRetryAsync<TEventDto>(
    //     string chainId,
    //     StreamingWebSocketClient client,
    //     string contractAddress,
    //     CancellationToken cancellationToken)
    //     where TEventDto : IEventDTO, new()
    // {
    //     return Task.Run(async () =>
    //     {
    //         var retryInterval = 1000;
    //         var handler = serviceProvider.GetRequiredService<IEvmEventProcessor<TEventDto>>();
    //
    //         while (!cancellationToken.IsCancellationRequested)
    //         {
    //             try
    //             {
    //                 await eventSubscriber.SubscribeEventAsync<TEventDto>(chainId, client,
    //                     async eventLog => await handler.HandleAsync(chainId, eventLog), contractAddress,cancellationToken);
    //                 break;
    //             }
    //             catch (Exception ex)
    //             {
    //                 Log.Error(ex,
    //                     "[EvmEventSubscriptionManager] ChainId: {ChainId}, Event: {EventType} subscription failed. Retrying...",
    //                     chainId, typeof(TEventDto).Name);
    //
    //                 retryInterval = Math.Min(retryInterval * 2, 60000);
    //                 await Task.Delay(retryInterval, cancellationToken);
    //             }
    //             finally
    //             {
    //                 try
    //                 {
    //                     if (client != null && client.IsStarted)
    //                     {
    //                         await client.StopAsync();
    //                         Log.Debug("[EvmEventSubscriptionManager] ChainId: {ChainId}, Event: {EventType} WebSocket client stopped. Restarting...",
    //                             chainId, typeof(TEventDto).Name);
    //                     }
    //                 }
    //                 catch (Exception stopEx)
    //                 {
    //                     Log.Warning(stopEx, "[EvmEventSubscriptionManager] Failed to stop WebSocket client.");
    //                 }
    //             }
    //         }
    //
    //         if (cancellationToken.IsCancellationRequested)
    //         {
    //             Log.Information("[EvmEventSubscriptionManager] Subscription canceled for {EventType} on {ChainId}",
    //                 typeof(TEventDto).Name, chainId);
    //         }
    //     }, cancellationToken);
}