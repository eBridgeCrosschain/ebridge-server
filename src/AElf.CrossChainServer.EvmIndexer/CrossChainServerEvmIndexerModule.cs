using AElf.CrossChainServer.EvmIndexer.Dtos.Event.Bridge;
using AElf.CrossChainServer.EvmIndexer.Processor.Bridge;
using AElf.CrossChainServer.Worker.EvmIndexerSync;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.BackgroundJobs.RabbitMQ;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace AElf.CrossChainServer.EvmIndexer;

[DependsOn(
    typeof(AbpBackgroundJobsRabbitMqModule)
)]
public class CrossChainServerEvmIndexerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<EvmContractSyncOptions>(configuration.GetSection("EvmContractSync"));

        context.Services.AddTransient<IEvmEventProcessor<NewReceiptEvent>, NewReceiptEventProcessor>();
        context.Services.AddTransient<IEvmEventProcessor<TokenSwappedEvent>, TokenSwappedEventProcessor>();
        context.Services.AddSingleton<IEvmEventSubscriptionManager, EvmEventSubscriptionManager>();
        context.Services.AddSingleton<IEventSubscriber, EventSubscriber>();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        context.AddBackgroundWorkerAsync<EvmIndexerHandlerWorker>();
    }
}
