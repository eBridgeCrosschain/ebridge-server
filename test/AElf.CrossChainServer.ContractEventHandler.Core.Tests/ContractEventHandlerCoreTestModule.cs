using AElf.AElfNode.EventHandler.TestBase;
using Volo.Abp.Modularity;

namespace AElf.CrossChainServer.ContractEventHandler
{
    [DependsOn(
        typeof(CrossChainServerApplicationTestModule),
        typeof(CrossChainServerContractEventHandlerCoreModule),
        typeof(AElfEventHandlerTestBaseModule)
    )]
    public class ContractEventHandlerCoreTestModule : AbpModule
    {

    }
}