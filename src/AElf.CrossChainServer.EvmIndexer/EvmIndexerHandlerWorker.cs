using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.EvmIndexer.Dtos.Event.Bridge;
using AElf.CrossChainServer.Worker.EvmIndexerSync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Reactive.Eth;
using Serilog;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.CrossChainServer.EvmIndexer;

public class EvmIndexerHandlerWorker
    : AsyncPeriodicBackgroundWorkerBase
{
    private readonly EvmContractSyncOptions _evmContractSyncOptions;
    private readonly IEvmEventSubscriptionManager _evmEventSubscriptionManager;
    private readonly Dictionary<string, StreamingWebSocketClient> _clients = new();

    public EvmIndexerHandlerWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IOptionsSnapshot<EvmContractSyncOptions> evmContractSyncOptions, IEvmEventSubscriptionManager evmEventSubscriptionManager) : base(
        timer,
        serviceScopeFactory)
    {
        _evmEventSubscriptionManager = evmEventSubscriptionManager;
        _evmContractSyncOptions = evmContractSyncOptions.Value;
        Timer.Period = _evmContractSyncOptions.SyncPeriod;
        foreach (var (chainId, indexerInfo) in _evmContractSyncOptions.IndexerInfos)
        {
            _clients[chainId] = new StreamingWebSocketClient(indexerInfo.WsUrl);
        }
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Log.Debug("[EvmIndexerHandlerWorker]Start handle evm transaction search");
        foreach (var kv in _clients)
        {
            var chainId = kv.Key;
            var client = kv.Value;

            if (!client.IsStarted)
            {
                Log.Warning(
                    $"[EvmIndexerHandlerWorker]{chainId} WebSocket is not started, connecting or reconnecting...");
                await InitializeWebSocketAsync(chainId, _evmContractSyncOptions.IndexerInfos[chainId]);
                continue;
            }

            try
            {
                Log.Information($"[EvmIndexerHandlerWorker]{chainId} Sending Ping...");

                var handler = new EthBlockNumberObservableHandler(client);
                handler.GetResponseAsObservable().Subscribe(blockNumber =>
                    Log.Information($"[EvmIndexerHandlerWorker]{chainId} Block Height: {blockNumber.Value}")
                );

                await handler.SendRequestAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[EvmIndexerHandlerWorker]{chainId} Ping failed, attempting reconnection...");
                await client.StopAsync();
                await InitializeWebSocketAsync(chainId, _evmContractSyncOptions.IndexerInfos[chainId]);
            }
        }
    }

    private async Task InitializeWebSocketAsync(string chainId, IndexerInfo indexerInfo)
    {
        if (_clients.TryGetValue(chainId, out var existingClient) && existingClient.IsStarted)
        {
            Log.Information($"[{chainId}] WebSocket already running, skipping initialization.");
            return;
        }

        Log.Information($"[{chainId}] Initializing WebSocket...");

        var client = new StreamingWebSocketClient(indexerInfo.WsUrl);
        _clients[chainId] = client;

        await client.StartAsync();
        var subscription = new List<ContractEventSubscriptionInfo>
        {
            new()
            {
                ContractAddress = indexerInfo.BridgeInContract,
                EventTypes =
                [
                    typeof(NewReceiptEvent)
                ]
            },
            new()
            {
                ContractAddress = indexerInfo.BridgeOutContract,
                EventTypes =
                [
                    typeof(TokenSwappedEvent)
                ]
            }
        };

        await _evmEventSubscriptionManager.SubscribeAsync(chainId,
            subscription, client, indexerInfo.PingDelay);
    }
}