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

public class UserLiquidityIndexerSyncProvider : IndexerSyncProviderBase
{
    private readonly IUserLiquidityInfoAppService _userLiquidityInfoAppService;
    private readonly ITokenAppService _tokenAppService;

    public UserLiquidityIndexerSyncProvider(IGraphQLClientFactory graphQlClientFactory, ISettingManager settingManager,
        IJsonSerializer jsonSerializer, IIndexerAppService indexerAppService, IChainAppService chainAppService,
        IUserLiquidityInfoAppService userLiquidityInfoAppService, ITokenAppService tokenAppService) : base(
        graphQlClientFactory, settingManager, jsonSerializer, indexerAppService, chainAppService)
    {
        _userLiquidityInfoAppService = userLiquidityInfoAppService;
        _tokenAppService = tokenAppService;
    }

    protected override string SyncType { get; } = CrossChainServerSettings.UserLiquidityIndexerSync;

    protected override async Task<long> HandleDataAsync(string aelfChainId, long startHeight, long endHeight)
    {
        Log.ForContext("chainId", aelfChainId).Debug(
            "Start to sync user liquidity info {chainId} from {StartHeight} to {EndHeight}",
            aelfChainId, startHeight, endHeight);
        var data = await QueryDataAsync<UserLiquidityRecordInfoDto>(GetRequest(aelfChainId, startHeight, endHeight));
        if (data == null || data.UserLiquidityRecordInfo.Count == 0)
        {
            return endHeight;
        }

        foreach (var userLiquidityRecord in data.UserLiquidityRecordInfo)
        {
            Log.ForContext("chainId", userLiquidityRecord.ChainId).Debug(
                "Start to handle user liquidity record info {ChainId},token {symbol},provider {provider}, liquidity type:{liquidityType}",
                userLiquidityRecord.ChainId, userLiquidityRecord.TokenSymbol, userLiquidityRecord.Provider,
                userLiquidityRecord.LiquidityType == LiquidityType.Add ? "Add" : "Remove");
            await HandleDataAsync(userLiquidityRecord);
        }

        return endHeight;
    }

    private async Task HandleDataAsync(UserLiquidityRecordInfo userLiquidityRecord)
    {
        var chain = await ChainAppService.GetByAElfChainIdAsync(
            ChainHelper.ConvertBase58ToChainId(userLiquidityRecord.ChainId));
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chain.Id,
            Symbol = userLiquidityRecord.TokenSymbol
        });
        var input = new UserLiquidityInfoInput
        {
            ChainId = chain.Id,
            Provider = userLiquidityRecord.Provider,
            Liquidity = userLiquidityRecord.Liquidity / (decimal)Math.Pow(10, token.Decimals),
            TokenId = token.Id
        };
        switch (userLiquidityRecord.LiquidityType)
        {
            case LiquidityType.Add:
                await _userLiquidityInfoAppService.AddUserLiquidityAsync(input);
                break;
            case LiquidityType.Remove:
                await _userLiquidityInfoAppService.RemoveUserLiquidityAsync(input);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(userLiquidityRecord.LiquidityType),
                    "Unsupported liquidity type.");
        }
    }

    private GraphQLRequest GetRequest(string chainId, long startHeight, long endHeight)
    {
        return new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxMaxResultCount:Int!){
            userLiquidityInfo(input: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxMaxResultCount:$maxMaxResultCount}){
                    id,
                    chainId,
                    blockHash,
                    blockHeight,
                    blockTime,
                    provider,
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

public class UserLiquidityRecordInfoDto
{
    public List<UserLiquidityRecordInfo> UserLiquidityRecordInfo { get; set; }
}

public class UserLiquidityRecordInfo : GraphQLDto
{
    public string Provider { get; set; }
    public string TokenSymbol { get; set; }
    public long Liquidity { get; set; }
    public LiquidityType LiquidityType { get; set; }
}

public enum LiquidityType
{
    Add,
    Remove
}