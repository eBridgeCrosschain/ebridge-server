using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AElf.Contracts.Oracle;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class SufficientCommitmentsCollectedProcessor : AElfEventProcessorBase<SufficientCommitmentsCollected>
{
    private readonly IOracleQueryInfoAppService _oracleQueryInfoAppService;
    private readonly IChainAppService _chainAppService;

    public SufficientCommitmentsCollectedProcessor(IOracleQueryInfoAppService oracleQueryInfoAppService, IChainAppService chainAppService)
    {
        _oracleQueryInfoAppService = oracleQueryInfoAppService;
        _chainAppService = chainAppService;
    }

    protected override async Task HandleEventAsync(SufficientCommitmentsCollected eventDetailsEto, EventContext txInfoDto)
    {
        var chain = await _chainAppService.GetByAElfChainIdAsync(txInfoDto.ChainId);
        await _oracleQueryInfoAppService.UpdateAsync(new UpdateOracleQueryInfoInput()
        {
            Step = OracleStep.SufficientCommitmentsCollected,
            ChainId = chain.Id,
            QueryId = eventDetailsEto.QueryId.ToHex(),
            LastUpdateHeight = txInfoDto.BlockNumber
        });
    }
}