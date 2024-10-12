using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Settings;
using GraphQL;
using GraphQL.Client.Abstractions;
using Volo.Abp.Json;

namespace AElf.CrossChainServer.Worker.IndexerSync;

public class ReportInfoIndexerSyncProvider : IndexerSyncProviderBase
{
    private readonly IReportInfoAppService _reportInfoAppService;

    public ReportInfoIndexerSyncProvider(IGraphQLClientFactory graphQlClientFactory, ISettingManager settingManager,
        IChainAppService chainAppService,IJsonSerializer jsonSerializer, IIndexerAppService indexerAppService,
        IReportInfoAppService reportInfoAppService) : base(
        graphQlClientFactory, settingManager,jsonSerializer,indexerAppService,chainAppService)
    {
        _reportInfoAppService = reportInfoAppService;
    }

    protected override string SyncType { get; } = CrossChainServerSettings.ReportIndexerSync;

    protected override async Task<long> HandleDataAsync(string aelfChainId, long startHeight, long endHeight)
    {
        var data = await QueryDataAsync<ReportInfoResponse>(GetRequest(aelfChainId, startHeight, endHeight));
        if (data == null || data.ReportInfo.Count == 0)
        {
            return endHeight;
        }

        foreach (var reportInfo in data.ReportInfo)
        {
            await HandleDataAsync(reportInfo);
        }

        return endHeight;
    }

    private async Task HandleDataAsync(ReportInfoDto report)
    {
        var chain = await ChainAppService.GetByAElfChainIdAsync(ChainHelper.ConvertBase58ToChainId(report.ChainId));

        switch (report.Step)
        {
            case ReportStep.Proposed:
                await _reportInfoAppService.CreateAsync(new CreateReportInfoInput
                {
                    ChainId = chain.Id,
                    ReceiptId = report.ReceiptId,
                    ReceiptHash = report.ReceiptHash,
                    RoundId = report.RoundId,
                    Token = report.Token,
                    TargetChainId = report.TargetChainId,
                    LastUpdateHeight = report.BlockHeight,
                    ReceiptInfo = report.ReceiptInfo
                });
                break;
            case ReportStep.Confirmed:
                await _reportInfoAppService.UpdateStepAsync(chain.Id, report.RoundId, report.Token,
                    report.TargetChainId, ReportStep.Confirmed, report.BlockHeight);
                break;
        }
    }

    private GraphQLRequest GetRequest(string chainId, long startHeight, long endHeight)
    {
        return new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxMaxResultCount:Int!){
            reportInfo(input: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxMaxResultCount:$maxMaxResultCount}){
                    id,
                    chainId,
                    blockHash,
                    blockHeight,
                    blockTime,
                    roundId,
                    token,
                    targetChainId,
                    receiptId,
                    receiptHash,
                    step,
                    receiptInfo                
            }
        }",
            Variables = new
            {
                chainId = chainId,
                startBlockHeight = startHeight,
                endBlockHeight = endHeight,
                maxMaxResultCount = MaxRequestCount
            }
        };
    }
}

public class ReportInfoResponse
{
    public List<ReportInfoDto> ReportInfo { get; set; } = new();
}

public class ReportInfoDto : GraphQLDto
{
    public long RoundId { get; set; }
    public string Token { get; set; }
    public string TargetChainId { get; set; }
    public string ReceiptId { get; set; }
    public string ReceiptHash { get; set; }
    public ReportStep Step { get; set; }
    public string ReceiptInfo { get; set; }

}