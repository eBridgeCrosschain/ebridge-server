using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.Tokens;
using GraphQL;
using Serilog;
using Volo.Abp.Json;

namespace AElf.CrossChainServer.Worker.IndexerSync;

public class PoolLiquidityIndexerSyncProvider : IndexerSyncProviderBase
{
    private readonly IPoolLiquidityInfoAppService _poolLiquidityInfoAppService;
    private readonly ITokenAppService _tokenAppService;

    public PoolLiquidityIndexerSyncProvider(IGraphQLClientFactory graphQlClientFactory, ISettingManager settingManager,
        IJsonSerializer jsonSerializer, IIndexerAppService indexerAppService, IChainAppService chainAppService,
        IPoolLiquidityInfoAppService poolLiquidityInfoAppService, ITokenAppService tokenAppService) : base(
        graphQlClientFactory, settingManager, jsonSerializer, indexerAppService, chainAppService)
    {
        _poolLiquidityInfoAppService = poolLiquidityInfoAppService;
        _tokenAppService = tokenAppService;
    }
    public override bool IsConfirmEnabled { get; set; } = false;
    protected override string SyncType { get; } = CrossChainServerSettings.PoolLiquidityIndexerSync;

    protected override async Task<long> HandleDataAsync(string aelfChainId, long startHeight, long endHeight)
    {
        Log.ForContext("chainId", aelfChainId).Debug(
            "Start to sync pool liquidity info {chainId} from {StartHeight} to {EndHeight}",
            aelfChainId, startHeight, endHeight);
        var data = await QueryDataAsync<PoolLiquidityRecordInfoDto>(GetRequest(aelfChainId, startHeight, endHeight));
        if (data == null || data.PoolLiquidityInfo.Count == 0)
        {
            return endHeight;
        }

        foreach (var poolLiquidityRecord in data.PoolLiquidityInfo)
        {
            Log.ForContext("chainId", poolLiquidityRecord.ChainId).Debug(
                "Start to handle pool liquidity record info {ChainId},token {symbol}, liquidity type:{liquidityType}",
                poolLiquidityRecord.ChainId, poolLiquidityRecord.TokenSymbol,
                poolLiquidityRecord.LiquidityType == LiquidityType.Add ? "Add" : "Remove");
            await HandleDataAsync(poolLiquidityRecord);
        }

        return endHeight;
    }

    private async Task HandleDataAsync(PoolLiquidityInfo poolLiquidityRecord)
    {
        var chain = await ChainAppService.GetByAElfChainIdAsync(
            ChainHelper.ConvertBase58ToChainId(poolLiquidityRecord.ChainId));
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chain.Id,
            Symbol = poolLiquidityRecord.TokenSymbol
        });
        var input = new PoolLiquidityInfoInput
        {
            ChainId = chain.Id,
            Liquidity = poolLiquidityRecord.Liquidity / (decimal)Math.Pow(10, token.Decimals),
            TokenId = token.Id
        };
        switch (poolLiquidityRecord.LiquidityType)
        {
            case LiquidityType.Add:
                await _poolLiquidityInfoAppService.AddLiquidityAsync(input);
                break;
            case LiquidityType.Remove:
                await _poolLiquidityInfoAppService.RemoveLiquidityAsync(input);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(poolLiquidityRecord.LiquidityType),
                    "Unsupported liquidity type.");
        }
    }

    private GraphQLRequest GetRequest(string chainId, long startHeight, long endHeight)
    {
        return new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxMaxResultCount:Int!){
            poolLiquidityInfo(input: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxMaxResultCount:$maxMaxResultCount}){
                    id,
                    chainId,
                    blockHash,
                    blockHeight,
                    blockTime,
                    tokenSymbol,
                    liquidity,
                    liquidityType
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

public class PoolLiquidityRecordInfoDto
{
    public List<PoolLiquidityInfo> PoolLiquidityInfo { get; set; }
}

public class PoolLiquidityInfo : GraphQLDto
{
    public string TokenSymbol { get; set; }
    public long Liquidity { get; set; }
    public LiquidityType LiquidityType { get; set; }
}