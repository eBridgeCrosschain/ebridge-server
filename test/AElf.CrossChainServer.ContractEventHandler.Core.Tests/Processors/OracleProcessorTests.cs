using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Contracts.Oracle;
using AElf.CrossChainServer.CrossChain;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class OracleProcessorTests : ContractEventHandlerCoreTestBase
{
    private readonly IEventHandlerTestProcessor<QueryCreated> _queryCreatedTestProcessor;
    private readonly IEventHandlerTestProcessor<Committed> _committedTestProcessor;
    private readonly IEventHandlerTestProcessor<SufficientCommitmentsCollected> _sufficientCommitmentsCollectedTestProcessor;
    private readonly IEventHandlerTestProcessor<CommitmentRevealed> _commitmentRevealedTestProcessor;
    private readonly IEventHandlerTestProcessor<QueryCompletedWithAggregation> _queryCompletedWithAggregationTestProcessor;
    private readonly IEventHandlerTestProcessor<QueryCompletedWithoutAggregation> _queryCompletedWithoutAggregationTestProcessor;
    private readonly IOracleQueryInfoAppService _oracleQueryInfoAppService;

    public OracleProcessorTests()
    {
        _queryCreatedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<QueryCreated>>();
        _committedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<Committed>>();
        _sufficientCommitmentsCollectedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<SufficientCommitmentsCollected>>();
        _commitmentRevealedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<CommitmentRevealed>>();
        _queryCompletedWithAggregationTestProcessor = GetRequiredService<IEventHandlerTestProcessor<QueryCompletedWithAggregation>>();
        _queryCompletedWithoutAggregationTestProcessor = GetRequiredService<IEventHandlerTestProcessor<QueryCompletedWithoutAggregation>>();
        _oracleQueryInfoAppService = GetRequiredService<IOracleQueryInfoAppService>();
    }

    [Fact]
    public async Task HandleEventTest()
    {
        var receiptHash = "ReceiptHash";
        var receiptId = $"{receiptHash}.1";
        var queryId = Hash.LoadFromHex("18a7d0f51ac07c3ec033e8bcc48e60a7723b1372f4bf584987d60884098cb14d");
        var queryEvent = new QueryCreated
        {
            QueryId = queryId,
            QueryInfo = new QueryInfo
            {
                Title = "record_price_elf",
                Options = { receiptHash }
            }
        };
        var contractEvent = EventContextHelper.Create("QueryCreated",9992731);
        await _queryCreatedTestProcessor.HandleEventAsync(queryEvent, contractEvent);
        
        var progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(0);
        
        queryEvent = new QueryCreated
        {
            QueryId = queryId,
            QueryInfo = new QueryInfo
            {
                Title = "record_receipts_elf",
                Options = { receiptId,receiptId }
            }
        };
        contractEvent = EventContextHelper.Create("QueryCreated",9992731);
        await _queryCreatedTestProcessor.HandleEventAsync(queryEvent, contractEvent);
        
        progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(20);
        
        var committedEvent = new Committed
        {
            QueryId = queryId,
        };
        contractEvent = EventContextHelper.Create("Committed",9992731);
        await _committedTestProcessor.HandleEventAsync(committedEvent, contractEvent);
        
        progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(40);
        
        var sufficientCommitmentsCollectedEvent = new SufficientCommitmentsCollected
        {
            QueryId = queryId,
        };
        contractEvent = EventContextHelper.Create("SufficientCommitmentsCollected",9992731);
        await _sufficientCommitmentsCollectedTestProcessor.HandleEventAsync(sufficientCommitmentsCollectedEvent, contractEvent);
        
        progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(60);
        
        var commitmentRevealedEvent = new CommitmentRevealed
        {
            QueryId = queryId,
        };
        contractEvent = EventContextHelper.Create("CommitmentRevealed",9992731);
        await _commitmentRevealedTestProcessor.HandleEventAsync(commitmentRevealedEvent, contractEvent);
        
        progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(80);
        
        var queryCompletedWithAggregationEvent = new QueryCompletedWithAggregation
        {
            QueryId = queryId,
        };
        contractEvent = EventContextHelper.Create("QueryCompletedWithAggregation",9992731);
        await _queryCompletedWithAggregationTestProcessor.HandleEventAsync(queryCompletedWithAggregationEvent, contractEvent);
        
        progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(100);
    }
    
    [Fact]
    public async Task HandleEventWithoutAggregationTest()
    {
        var receiptHash = "ReceiptHash";
        var receiptId = $"{receiptHash}.1";
        var queryId = Hash.LoadFromHex("18a7d0f51ac07c3ec033e8bcc48e60a7723b1372f4bf584987d60884098cb14d");
        var queryEvent = new QueryCreated
        {
            QueryId = queryId,
            QueryInfo = new QueryInfo
            {
                Title = "record_price_elf",
                Options = { receiptId }
            }
        };
        var contractEvent = EventContextHelper.Create("QueryCreated",9992731);
        await _queryCreatedTestProcessor.HandleEventAsync(queryEvent, contractEvent);
        
        var progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(0);
        
        queryEvent = new QueryCreated
        {
            QueryId = queryId,
            QueryInfo = new QueryInfo
            {
                Title = "record_receipts_elf",
                Options = { receiptId,receiptId }
            }
        };
        contractEvent = EventContextHelper.Create("QueryCreated",9992731);
        await _queryCreatedTestProcessor.HandleEventAsync(queryEvent, contractEvent);
        
        progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(20);
        
        var committedEvent = new Committed
        {
            QueryId = queryId,
        };
        contractEvent = EventContextHelper.Create("Committed",9992731);
        await _committedTestProcessor.HandleEventAsync(committedEvent, contractEvent);
        
        progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(40);
        
        var sufficientCommitmentsCollectedEvent = new SufficientCommitmentsCollected
        {
            QueryId = queryId,
        };
        contractEvent = EventContextHelper.Create("SufficientCommitmentsCollected",9992731);
        await _sufficientCommitmentsCollectedTestProcessor.HandleEventAsync(sufficientCommitmentsCollectedEvent, contractEvent);
        
        progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(60);
        
        var commitmentRevealedEvent = new CommitmentRevealed
        {
            QueryId = queryId,
        };
        contractEvent = EventContextHelper.Create("CommitmentRevealed",9992731);
        await _commitmentRevealedTestProcessor.HandleEventAsync(commitmentRevealedEvent, contractEvent);
        
        progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(80);
        
        var queryCompletedWithAggregationEvent = new QueryCompletedWithoutAggregation
        {
            QueryId = queryId,
        };
        contractEvent = EventContextHelper.Create("QueryCompletedWithoutAggregation",9992731);
        await _queryCompletedWithoutAggregationTestProcessor.HandleEventAsync(queryCompletedWithAggregationEvent, contractEvent);
        
        progress = await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(100);
    }
}