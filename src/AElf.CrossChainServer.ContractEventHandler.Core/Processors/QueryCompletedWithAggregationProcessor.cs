using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AElf.Contracts.Oracle;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class QueryCompletedWithAggregationProcessor : AElfEventProcessorBase<QueryCompletedWithAggregation>
{
    private readonly IOracleQueryInfoAppService _oracleQueryInfoAppService;
    private readonly IChainAppService _chainAppService;

    public QueryCompletedWithAggregationProcessor(IOracleQueryInfoAppService oracleQueryInfoAppService, IChainAppService chainAppService)
    {
        _oracleQueryInfoAppService = oracleQueryInfoAppService;
        _chainAppService = chainAppService;
    }

    protected override async Task HandleEventAsync(QueryCompletedWithAggregation eventDetailsEto, EventContext txInfoDto)
    {
        var chain = await _chainAppService.GetByAElfChainIdAsync(txInfoDto.ChainId);
        await _oracleQueryInfoAppService.UpdateAsync(new UpdateOracleQueryInfoInput()
        {
            Step = OracleStep.QueryCompleted,
            ChainId = chain.Id,
            QueryId = eventDetailsEto.QueryId.ToHex(),
            UpdateTime = txInfoDto.BlockTime
        });
    }
}