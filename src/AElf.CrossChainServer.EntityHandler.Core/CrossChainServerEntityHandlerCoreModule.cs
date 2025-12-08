using Volo.Abp.Modularity;

namespace AElf.CrossChainServer.EntityHandler.Core
{
    [DependsOn(
        typeof(CrossChainServerApplicationContractsModule)
    )]
    public class CrossChainServerEntityHandlerCoreModule: AbpModule
    {
    }
}