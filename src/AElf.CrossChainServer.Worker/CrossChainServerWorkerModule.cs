using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.Worker.EvmIndexerSync;
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
        typeof(AbpBackgroundWorkersModule))]
    public class CrossChainServerWorkerModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<BridgeContractSyncOptions>(configuration.GetSection("BridgeContractSync"));
            Configure<TonIndexSyncOptions>(configuration.GetSection("TonIndexSync"));
            Configure<EvmContractSyncOptions>(configuration.GetSection("EvmContractSync"));
                        
            context.Services.AddTransient<IEvmSyncProvider, EvmTokenPoolIndexerSyncProvider>();
            context.Services.AddTransient<IEvmSyncProvider, EvmNewReceiptSyncProvider>();
            context.Services.AddTransient<IEvmSyncProvider, EvmTokenSwapSyncProvider>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var liquiditySyncOption = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<PoolLiquiditySyncOptions>>();
            if (liquiditySyncOption.Value.IsSyncEnabled)
            {
                var service = context.ServiceProvider.GetRequiredService<IPoolLiquidityInfoAppService>();
                AsyncHelper.RunSync(async()=> await service.SyncPoolLiquidityInfoFromChainAsync());
            }
            context.AddBackgroundWorkerAsync<TransferProgressUpdateWorker>();
            context.AddBackgroundWorkerAsync<CrossChainIndexingCleanWorker>();
            context.AddBackgroundWorkerAsync<TransferAutoReceiveWorker>();
            context.AddBackgroundWorkerAsync<IndexerSyncWorker>();
            context.AddBackgroundWorkerAsync<CheckReceiveWorker>();
            context.AddBackgroundWorkerAsync<TonIndexSyncWorker>();
            context.AddBackgroundWorkerAsync<EvmIndexerSyncWorker>();
        }
    }
}