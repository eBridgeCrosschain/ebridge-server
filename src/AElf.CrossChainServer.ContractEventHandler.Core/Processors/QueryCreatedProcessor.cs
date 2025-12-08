using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AElf.Contracts.Oracle;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class QueryCreatedProcessor: AElfEventProcessorBase<QueryCreated>
{
    private readonly IOracleQueryInfoAppService _oracleQueryInfoAppService;
    private readonly IChainAppService _chainAppService;

    public QueryCreatedProcessor(IOracleQueryInfoAppService oracleQueryInfoAppService, IChainAppService chainAppService)
    {
        _oracleQueryInfoAppService = oracleQueryInfoAppService;
        _chainAppService = chainAppService;
    }

    protected override async Task HandleEventAsync(QueryCreated eventDetailsEto, EventContext txInfoDto)
    {
        if (!eventDetailsEto.QueryInfo.Title.StartsWith("record_receipts"))
        {
            return;
        }

        var chain = await _chainAppService.GetByAElfChainIdAsync(txInfoDto.ChainId);
        var receiptHash = eventDetailsEto.QueryInfo.Options[0].Split(".")[0];
        var starIndex = Convert.ToInt64(eventDetailsEto.QueryInfo.Options[0].Split(".")[1]);
        var endIndex = Convert.ToInt64(eventDetailsEto.QueryInfo.Options[1].Split(".")[1]);

        for (var i = starIndex; i <= endIndex; i++)
        {
            await _oracleQueryInfoAppService.CreateAsync(new CreateOracleQueryInfoInput
            {
                Option = $"{receiptHash}.{i}",
                Step = OracleStep.QueryCreated,
                ChainId = chain.Id,
                QueryId = eventDetailsEto.QueryId.ToHex(),
                LastUpdateHeight = txInfoDto.BlockNumber
            });
        }
    }
}