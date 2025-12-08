using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.Core;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElf.CrossChainServer.ContractEventHandler
{
    [DependsOn(
        typeof(CrossChainServerApplicationModule),
        typeof(AElfNodeEventHandlerCoreModule),
        typeof(AElfEventHandlerBackgroundJobModule),
        typeof(CrossChainServerDomainSharedModule)
    )]
    public class CrossChainServerContractEventHandlerCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<CrossChainServerContractEventHandlerCoreModule>();
            });
        }
    }
}