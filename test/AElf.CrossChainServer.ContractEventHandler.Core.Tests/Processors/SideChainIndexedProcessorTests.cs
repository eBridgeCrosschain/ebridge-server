using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Contracts.CrossChain;
using AElf.CrossChainServer.CrossChain;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class SideChainIndexedProcessorTests : ContractEventHandlerCoreTestBase
{
    private readonly IEventHandlerTestProcessor<SideChainIndexed> _sideChainIndexedTestProcessor;
    private readonly ICrossChainIndexingInfoRepository _crossChainIndexingInfoRepository;

    public SideChainIndexedProcessorTests()
    {
        _sideChainIndexedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<SideChainIndexed>>();
        _crossChainIndexingInfoRepository = GetRequiredService<ICrossChainIndexingInfoRepository>();
    }

    [Fact]
    public async Task HandleEventTest()
    {
        var @event = new SideChainIndexed
        {
            ChainId = 1866392,
            IndexedHeight = 100
        };
        var contractEvent = EventContextHelper.Create("SideChainIndexed",9992731);
        
        await _sideChainIndexedTestProcessor.HandleEventAsync(@event, contractEvent);

        var indexes = await _crossChainIndexingInfoRepository.GetListAsync();
        indexes[0].IndexChainId.ShouldBe("SideChain_tDVV");
        indexes[0].IndexBlockHeight.ShouldBe(@event.IndexedHeight);
        indexes[0].BlockHeight.ShouldBe(contractEvent.BlockNumber);
        indexes[0].BlockTime.ShouldBe(contractEvent.BlockTime);
        
        var @event2 = new SideChainIndexed
        {
            ChainId = 1,
            IndexedHeight = 200
        };
        var contractEvent2 = EventContextHelper.Create("SideChainIndexed",9992731);
        
        await _sideChainIndexedTestProcessor.HandleEventAsync(@event2, contractEvent2);

        indexes = await _crossChainIndexingInfoRepository.GetListAsync();
        indexes.Count.ShouldBe(1);
        indexes[0].IndexChainId.ShouldBe("SideChain_tDVV");
        indexes[0].IndexBlockHeight.ShouldBe(@event.IndexedHeight);
        indexes[0].BlockHeight.ShouldBe(contractEvent.BlockNumber);
        indexes[0].BlockTime.ShouldBe(contractEvent.BlockTime);
    }
}