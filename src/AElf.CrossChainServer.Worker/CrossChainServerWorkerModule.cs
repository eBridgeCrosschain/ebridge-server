using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.EvmIndexer;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.Worker.EvmIndexerSync;
using AElf.CrossChainServer.Worker.EvmIndexerSync.Limit;
using AElf.CrossChainServer.Worker.IndexerSync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.CrossChainServer.Worker
{
    [DependsOn(
        typeof(AbpBackgroundWorkersModule), typeof(CrossChainServerEvmIndexerModule))]
    public class CrossChainServerWorkerModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<BridgeContractSyncOptions>(configuration.GetSection("BridgeContractSync"));
            Configure<TonIndexSyncOptions>(configuration.GetSection("TonIndexSync"));
            Configure<EvmContractSyncOptions>(configuration.GetSection("EvmContractSync"));
            Configure<TokenLimitSwapInfoOptions>(configuration.GetSection("TokenLimitSwapInfo"));

            context.Services.AddTransient<IEvmSyncProvider, EvmTokenPoolIndexerSyncProvider>();
            context.Services.AddTransient<IEvmSyncProvider, EvmNewReceiptSyncProvider>();
            context.Services.AddTransient<IEvmSyncProvider, EvmTokenSwapSyncProvider>();
            context.Services.AddTransient<IEvmSyncProvider, EvmDailyLimitSetSyncProvider>();
            context.Services.AddTransient<IEvmSyncProvider, EvmDailyLimitConsumedSyncProvider>();
            context.Services.AddTransient<IEvmSyncProvider, EvmRateLimitConsumedSyncProvider>();
            context.Services.AddTransient<IEvmSyncProvider, EvmRateLimitSetSyncProvider>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var liquiditySyncOption =
                context.ServiceProvider.GetRequiredService<IOptionsSnapshot<PoolLiquiditySyncOptions>>();
            if (liquiditySyncOption.Value.IsSyncEnabled)
            {
                var service = context.ServiceProvider.GetRequiredService<IPoolLiquidityInfoAppService>();
                AsyncHelper.RunSync(async () => await service.SyncPoolLiquidityInfoFromChainAsync());
            }

            var limitSyncOption = 
                context.ServiceProvider.GetRequiredService<IOptionsSnapshot<LimitSyncOptions>>();
            if (limitSyncOption.Value.IsSyncEnabled)
            {
                var service = context.ServiceProvider.GetRequiredService<ICrossChainLimitAppService>();
                AsyncHelper.RunSync(async () => await service.InitLimitAsync());
            }
            {
                var bridgeContractSyncOptions = context.ServiceProvider
                    .GetRequiredService<IOptionsSnapshot<BridgeContractSyncOptions>>();
                if (bridgeContractSyncOptions.Value.EnableSyncHeight)
                {
                    var service = context.ServiceProvider.GetRequiredService<ISettingManager>();
                    var chainService = context.ServiceProvider.GetRequiredService<IChainAppService>();
                    AsyncHelper.RunSync(async () =>
                    {
                        var chains = await chainService.GetListAsync(new GetChainsInput
                        {
                            Type = BlockchainType.AElf
                        });
                        foreach (var chain in chains.Items)
                        {
                            var syncDelayHeight = await service.GetOrNullAsync(chain.Id,
                                CrossChainServerSettings.PoolLiquidityIndexerSync);
                            await service.SetAsync(chain.Id,
                                $"{bridgeContractSyncOptions.Value.ConfirmedSyncKeyPrefix}-{CrossChainServerSettings.PoolLiquidityIndexerSync}",
                                syncDelayHeight);
                            var syncDelayHeightUserLiq = await service.GetOrNullAsync(chain.Id,
                                CrossChainServerSettings.UserLiquidityIndexerSync);
                            await service.SetAsync(chain.Id,
                                $"{bridgeContractSyncOptions.Value.ConfirmedSyncKeyPrefix}-{CrossChainServerSettings.UserLiquidityIndexerSync}",
                                syncDelayHeightUserLiq);
                        }
                    });
                }
            }
            context.AddBackgroundWorkerAsync<TransferProgressUpdateWorker>();
            context.AddBackgroundWorkerAsync<CrossChainIndexingCleanWorker>();
            context.AddBackgroundWorkerAsync<TransferAutoReceiveWorker>();
            context.AddBackgroundWorkerAsync<IndexerSyncWorker>();
            // context.AddBackgroundWorkerAsync<CheckReceiveWorker>();
            context.AddBackgroundWorkerAsync<TonIndexSyncWorker>();
            context.AddBackgroundWorkerAsync<EvmIndexerSyncWorker>();
            context.AddBackgroundWorkerAsync<CheckEvmTransactionConfirmedWorker>();
            context.AddBackgroundWorkerAsync<CheckAElfConfirmedTransactionWorker>();
        }
    }
}