using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Contracts.Report;
using AElf.CrossChainServer.CrossChain;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class ReportProcessorTest : ContractEventHandlerCoreTestBase
{
    private readonly IEventHandlerTestProcessor<ReportProposed> _reportProposedTestProcessor;
    private readonly IEventHandlerTestProcessor<ReportConfirmed> _reportConfirmedTestProcessor;
    private readonly IReportInfoAppService _reportInfoAppService;

    public ReportProcessorTest()
    {
        _reportProposedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<ReportProposed>>();
        _reportConfirmedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<ReportConfirmed>>();
        _reportInfoAppService = GetRequiredService<IReportInfoAppService>();
    }

    [Fact]
    public async Task HandleEventTest()
    {
        var receiptId = "ReceiptId";
        var queryEvent = new ReportProposed
        {
            QueryInfo = new OffChainQueryInfo
            {
                Title = "record_price_",
                Options = { receiptId }
            },
            Token = "token_address",
            RoundId = 1
        };
        var contractEvent = EventContextHelper.Create("ReportProposed",9992731);
        await _reportProposedTestProcessor.HandleEventAsync(queryEvent, contractEvent);
        
        var progress = await _reportInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(0);
        
        queryEvent = new ReportProposed
        {
            QueryInfo = new OffChainQueryInfo
            {
                Title = "lock_token_"+receiptId,
                Options = { receiptId }
            },
            Token = "token_address",
            RoundId = 1,
            TargetChainId = "Ethereum"
        };
        contractEvent = EventContextHelper.Create("ReportProposed",9992731);
        await _reportProposedTestProcessor.HandleEventAsync(queryEvent, contractEvent);
        
        progress = await _reportInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(100/3);

        var reportConfirmedEvent = new ReportConfirmed
        {
            RoundId = 1,
            Token = "token_address",
            TargetChainId = "Ethereum",
        };
        contractEvent = EventContextHelper.Create("ReportConfirmed",9992731);
        await _reportConfirmedTestProcessor.HandleEventAsync(reportConfirmedEvent, contractEvent);
        
        progress = await _reportInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(100/3);
        
        reportConfirmedEvent = new ReportConfirmed
        {
            RoundId = 1,
            Token = "token_address",
            TargetChainId = "Ethereum",
            IsAllNodeConfirmed = true
        };
        contractEvent = EventContextHelper.Create("ReportConfirmed",9992731);
        await _reportConfirmedTestProcessor.HandleEventAsync(reportConfirmedEvent, contractEvent);
        
        progress = await _reportInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF",receiptId);
        progress.ShouldBe(200/3);
    }
}