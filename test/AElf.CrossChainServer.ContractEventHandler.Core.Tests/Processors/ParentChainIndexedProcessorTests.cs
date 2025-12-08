using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Contracts.CrossChain;
using AElf.CrossChainServer.CrossChain;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class ParentChainIndexedProcessorTests : ContractEventHandlerCoreTestBase
{
    private readonly IEventHandlerTestProcessor<ParentChainIndexed> _parentChainIndexedTestProcessor;
    private readonly ICrossChainIndexingInfoRepository _crossChainIndexingInfoRepository;

    public ParentChainIndexedProcessorTests()
    {
        _parentChainIndexedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<ParentChainIndexed>>();
        _crossChainIndexingInfoRepository = GetRequiredService<ICrossChainIndexingInfoRepository>();
    }

    [Fact]
    public async Task HandleEventTest()
    {
        var @event = new ParentChainIndexed
        {
            ChainId = 9992731,
            IndexedHeight = 100
        };
        var contractEvent = EventContextHelper.Create("ParentChainIndexed",1866392);
        
        await _parentChainIndexedTestProcessor.HandleEventAsync(@event, contractEvent);

        var indexes = await _crossChainIndexingInfoRepository.GetListAsync();
        indexes[0].IndexChainId.ShouldBe("MainChain_AELF");
        indexes[0].IndexBlockHeight.ShouldBe(@event.IndexedHeight);
        indexes[0].BlockHeight.ShouldBe(contractEvent.BlockNumber);
        indexes[0].BlockTime.ShouldBe(contractEvent.BlockTime);
        
        var @event2 = new ParentChainIndexed
        {
            ChainId = 1,
            IndexedHeight = 200
        };
        var contractEvent2 = EventContextHelper.Create("ParentChainIndexed",1866392);
        
        await _parentChainIndexedTestProcessor.HandleEventAsync(@event2, contractEvent2);

        indexes = await _crossChainIndexingInfoRepository.GetListAsync();
        indexes[0].IndexChainId.ShouldBe("MainChain_AELF");
        indexes[0].IndexBlockHeight.ShouldBe(@event.IndexedHeight);
        indexes[0].BlockHeight.ShouldBe(contractEvent.BlockNumber);
        indexes[0].BlockTime.ShouldBe(contractEvent.BlockTime);
    }
}