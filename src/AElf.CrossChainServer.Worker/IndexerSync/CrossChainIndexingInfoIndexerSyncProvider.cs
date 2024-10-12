using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Settings;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Core;
using Volo.Abp.Json;

namespace AElf.CrossChainServer.Worker.IndexerSync;

public class CrossChainIndexingInfoIndexerSyncProvider : IndexerSyncProviderBase
{
    private readonly ICrossChainIndexingInfoAppService _crossChainIndexingInfoAppService;
    public ILogger<CrossChainIndexingInfoIndexerSyncProvider> Logger { get; set; }


    public CrossChainIndexingInfoIndexerSyncProvider(IGraphQLClientFactory graphQlClientFactory,
        ISettingManager settingManager,
        IChainAppService chainAppService, IJsonSerializer jsonSerializer, IIndexerAppService indexerAppService,
        ICrossChainIndexingInfoAppService crossChainIndexingInfoAppService) : base(
        graphQlClientFactory, settingManager, jsonSerializer, indexerAppService, chainAppService)
    {
        _crossChainIndexingInfoAppService = crossChainIndexingInfoAppService;
        Logger = NullLogger<CrossChainIndexingInfoIndexerSyncProvider>.Instance;
    }

    protected override string SyncType { get; } = CrossChainServerSettings.CrossChainIndexingIndexerSync;

    protected override async Task<long> HandleDataAsync(string aelfChainId, long startHeight, long endHeight)
    {
        Logger.LogDebug("Start to sync cross chain indexing info {ChainId} from {StartHeight} to {EndHeight}",
            aelfChainId, startHeight, endHeight);
        var data = await QueryDataAsync<CrossChainIndexingInfoResponse>(GetRequest(aelfChainId, startHeight,
            endHeight));
        if (data == null || data.CrossChainIndexingInfo.Count == 0)
        {
            return endHeight;
        }

        foreach (var indexing in data.CrossChainIndexingInfo)
        {
            await HandleDataAsync(indexing);
        }

        return endHeight;
    }

    private async Task HandleDataAsync(CrossChainIndexingInfoDto data)
    {
        Logger.LogDebug(
            "Start to handler cross chain indexing info {ChainId},index chain id:{indexChainId},index height:{indexHeight}",
            data.ChainId, data.IndexChainId, data.IndexBlockHeight);
        var chain = await ChainAppService.GetByAElfChainIdAsync(ChainHelper.ConvertBase58ToChainId(data.ChainId));
        var indexChain =
            await ChainAppService.GetByAElfChainIdAsync(ChainHelper.ConvertBase58ToChainId(data.IndexChainId));
        if (indexChain == null)
        {
            return;
        }

        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            BlockHeight = data.BlockHeight,
            BlockTime = data.BlockTime,
            ChainId = chain.Id,
            IndexChainId = indexChain.Id,
            IndexBlockHeight = data.IndexBlockHeight
        });
    }

    private GraphQLRequest GetRequest(string chainId, long startHeight, long endHeight)
    {
        return new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxMaxResultCount:Int!){
            crossChainIndexingInfo(input: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxMaxResultCount:$maxMaxResultCount}){
                    id,
                    chainId,
                    blockHash,
                    blockHeight,
                    blockTime,
                    indexChainId,
                    indexBlockHeight
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

public class CrossChainIndexingInfoResponse
{
    public List<CrossChainIndexingInfoDto> CrossChainIndexingInfo { get; set; } = new();
}

public class CrossChainIndexingInfoDto : GraphQLDto
{
    public string IndexChainId { get; set; }
    public long IndexBlockHeight { get; set; }
}