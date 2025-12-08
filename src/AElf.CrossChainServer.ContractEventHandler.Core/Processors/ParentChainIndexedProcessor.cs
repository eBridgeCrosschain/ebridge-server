using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AElf.Contracts.CrossChain;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class ParentChainIndexedProcessor: AElfEventProcessorBase<ParentChainIndexed>
{
    private readonly ICrossChainIndexingInfoAppService _crossChainIndexingInfoAppService;
    private readonly IChainAppService _chainAppService;

    public ParentChainIndexedProcessor(ICrossChainIndexingInfoAppService crossChainIndexingInfoAppService,
        IChainAppService chainAppService)
    {
        _crossChainIndexingInfoAppService = crossChainIndexingInfoAppService;
        _chainAppService = chainAppService;
    }

    protected override async Task HandleEventAsync(ParentChainIndexed eventDetailsEto, EventContext txInfoDto)
    {
        var chain = await _chainAppService.GetByAElfChainIdAsync(txInfoDto.ChainId);
        var indexChain = await _chainAppService.GetByAElfChainIdAsync(eventDetailsEto.ChainId);
        if (indexChain == null)
        {
            return;
        }

        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            BlockHeight = txInfoDto.BlockNumber,
            BlockTime = txInfoDto.BlockTime,
            ChainId = chain.Id,
            IndexChainId = indexChain.Id,
            IndexBlockHeight = eventDetailsEto.IndexedHeight
        });
    }
}