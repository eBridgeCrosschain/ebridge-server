using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.Tokens;
using AElf.CrossChainServer.Worker.EvmIndexerSync;
using GraphQL;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.Json;

namespace AElf.CrossChainServer.Worker.IndexerSync;

public class UserLiquidityIndexerSyncProvider : IndexerSyncProviderBase
{
    private readonly IUserLiquidityInfoAppService _userLiquidityInfoAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly EvmContractSyncOptions _evmContractSyncOptions;

    public UserLiquidityIndexerSyncProvider(IGraphQLClientFactory graphQlClientFactory, ISettingManager settingManager,
        IJsonSerializer jsonSerializer, IIndexerAppService indexerAppService, IChainAppService chainAppService,
        IUserLiquidityInfoAppService userLiquidityInfoAppService, ITokenAppService tokenAppService,IOptionsSnapshot<EvmContractSyncOptions> evmContractSyncOptions) : base(
        graphQlClientFactory, settingManager, jsonSerializer, indexerAppService, chainAppService)
    {
        _userLiquidityInfoAppService = userLiquidityInfoAppService;
        _tokenAppService = tokenAppService;
        _evmContractSyncOptions = evmContractSyncOptions.Value;
    }

    public override bool RequiresRealTime { get; set; } = false;
    protected override string SyncType { get; } = CrossChainServerSettings.UserLiquidityIndexerSync;

    protected override async Task<long> HandleDataAsync(string aelfChainId, long startHeight, long endHeight)
    {
        if (!_evmContractSyncOptions.Enabled)
        {
            return 0;
        }
        Log.ForContext("chainId", aelfChainId).Debug(
            "Start to sync user liquidity info {chainId} from {StartHeight} to {EndHeight}",
            aelfChainId, startHeight, endHeight);
        var data = await QueryDataAsync<UserLiquidityRecordInfoDto>(GetRequest(aelfChainId, startHeight, endHeight));
        if (data == null || data.UserLiquidityInfo.Count == 0)
        {
            return endHeight;
        }

        foreach (var userLiquidityRecord in data.UserLiquidityInfo)
        {
            Log.ForContext("chainId", userLiquidityRecord.ChainId).Debug(
                "Start to handle user liquidity record info {ChainId},token {symbol},provider {provider}, liquidity type:{liquidityType}",
                userLiquidityRecord.ChainId, userLiquidityRecord.TokenSymbol, userLiquidityRecord.Provider,
                userLiquidityRecord.LiquidityType == LiquidityType.Add ? "Add" : "Remove");
            await HandleDataAsync(userLiquidityRecord);
        }

        return endHeight;
    }

    private async Task HandleDataAsync(UserLiquidityInfo userLiquidity)
    {
        var chain = await ChainAppService.GetByAElfChainIdAsync(
            ChainHelper.ConvertBase58ToChainId(userLiquidity.ChainId));
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chain.Id,
            Symbol = userLiquidity.TokenSymbol
        });
        var input = new UserLiquidityInfoInput
        {
            ChainId = chain.Id,
            Provider = userLiquidity.Provider,
            Liquidity = userLiquidity.Liquidity / (decimal)Math.Pow(10, token.Decimals),
            TokenId = token.Id
        };
        switch (userLiquidity.LiquidityType)
        {
            case LiquidityType.Add:
                await _userLiquidityInfoAppService.AddUserLiquidityAsync(input);
                break;
            case LiquidityType.Remove:
                await _userLiquidityInfoAppService.RemoveUserLiquidityAsync(input);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(userLiquidity.LiquidityType),
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
    public List<UserLiquidityInfo> UserLiquidityInfo { get; set; }
}

public class UserLiquidityInfo : GraphQLDto
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