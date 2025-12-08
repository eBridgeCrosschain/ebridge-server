using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AElf.Contracts.Report;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class ReportProposedProcessor: AElfEventProcessorBase<ReportProposed>
{
    private readonly IReportInfoAppService _reportInfoAppService;
    private readonly IChainAppService _chainAppService;

    public ReportProposedProcessor(IReportInfoAppService reportInfoAppService, 
        IChainAppService chainAppService)
    {
        _reportInfoAppService = reportInfoAppService;
        _chainAppService = chainAppService;
    }

    protected override async Task HandleEventAsync(ReportProposed eventDetailsEto, EventContext txInfoDto)
    {
        if (!eventDetailsEto.QueryInfo.Title.StartsWith("lock_token_"))
        {
            return;
        }
        
        var chain = await _chainAppService.GetByAElfChainIdAsync(txInfoDto.ChainId);
        await _reportInfoAppService.CreateAsync(new CreateReportInfoInput
        {
            ChainId = chain.Id,
            ReceiptId = eventDetailsEto.QueryInfo.Title.Split("_")[2],
            ReceiptHash = eventDetailsEto.QueryInfo.Options[0],
            RoundId = eventDetailsEto.RoundId,
            Token = eventDetailsEto.Token,
            TargetChainId = eventDetailsEto.TargetChainId,
            LastUpdateHeight = txInfoDto.BlockNumber
        });
    }
}