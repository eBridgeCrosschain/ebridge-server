using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AElf.Contracts.Report;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Tokens;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class ReportConfirmedProcessor: AElfEventProcessorBase<ReportConfirmed>
{
    private readonly IReportInfoAppService _reportInfoAppService;
    private readonly IChainAppService _chainAppService;
    private readonly ITokenAppService _tokenAppService;

    public ReportConfirmedProcessor(IReportInfoAppService reportInfoAppService, ITokenAppService tokenAppService, IChainAppService chainAppService)
    {
        _reportInfoAppService = reportInfoAppService;
        _tokenAppService = tokenAppService;
        _chainAppService = chainAppService;
    }

    protected override async Task HandleEventAsync(ReportConfirmed eventDetailsEto, EventContext txInfoDto)
    {
        if (!eventDetailsEto.IsAllNodeConfirmed)
        {
            return;
        }
        var chain = await _chainAppService.GetByAElfChainIdAsync(txInfoDto.ChainId);
        await _reportInfoAppService.UpdateStepAsync(chain.Id, eventDetailsEto.RoundId, eventDetailsEto.Token,
            eventDetailsEto.TargetChainId, ReportStep.Confirmed, txInfoDto.BlockNumber);
    }
}