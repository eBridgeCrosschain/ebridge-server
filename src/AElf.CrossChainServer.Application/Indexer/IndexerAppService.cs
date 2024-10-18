using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.HttpClient;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;

namespace AElf.CrossChainServer.Indexer;

[RemoteService(IsEnabled = false)]
public class IndexerAppService: CrossChainServerAppService, IIndexerAppService
{
    private readonly IGraphQLClient _graphQlClient;
    private readonly IChainAppService _chainAppService;
    private readonly IHttpProvider _httpProvider;
    private readonly SyncStateServiceOption _syncStateServiceOption;
    private ApiInfo _syncStateUri => new (HttpMethod.Get, _syncStateServiceOption.SyncStateUri);

    public IndexerAppService(IGraphQLClientFactory graphQlClientFactory, IChainAppService chainAppService, IHttpProvider httpProvider, 
        IOptionsSnapshot<SyncStateServiceOption> syncStateServiceOption)
    {
        _graphQlClient = graphQlClientFactory.GetClient(GraphQLClientEnum.CrossChainServerClient);
        _chainAppService = chainAppService;
        _httpProvider = httpProvider;
        _syncStateServiceOption = syncStateServiceOption.Value;
    }

    // todo:exception
    public async Task<long> GetLatestIndexHeightAsync(string chainId)
    {
        var chain = await _chainAppService.GetAsync(chainId);
        if (chain == null)
        {
            return 0;
        }
        try
        {
            var aelfChainId= ChainHelper.ConvertChainIdToBase58(chain.AElfChainId);
            var res = await _httpProvider.InvokeAsync<SyncStateResponse>(_syncStateServiceOption.BaseUrl, _syncStateUri);
            var blockHeight= res.CurrentVersion.Items.FirstOrDefault(i => i.ChainId == aelfChainId)?.LastIrreversibleBlockHeight;
            Logger.LogInformation("Get latest index height. chainId: {chainId}, height: {height}",aelfChainId,blockHeight);
            return blockHeight ?? 0;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Query swap syncState error");
            return 0;
        }
    }

    // todo:log
    public async Task<CrossChainTransferInfoDto> GetPendingTransactionAsync(string chainId,string transferTransactionId)
    {
        var data = await QueryDataAsync<GraphQLResponse<CrossChainTransferInfoDto>>(GetRequest(chainId, transferTransactionId));
        if (data == null)
        {
            Logger.LogInformation("Get pending transaction failed. chainId: {chainId}, transferTransactionId: {transferTransactionId}",chainId,transferTransactionId);
            return null;
        }
        Logger.LogInformation("Get pending transaction success. chainId: {chainId}, transferTransactionId: {transferTransactionId}, data: {data}",chainId,transferTransactionId, JsonConvert.SerializeObject(data.Data));
        return data.Data;
    }
    
    private GraphQLRequest GetRequest(string chainId, string transactionId)
    {
        return new GraphQLRequest
        {
            Query =
                @"query(
                    $chainId:String,
                    $transactionId:String
                ) {
                    data:homogeneousCrossChainTransferInfo(
                        input: {
                            chainId:$chainId,
                            transactionId:$transactionId
                        }
                    ){
                    id,
                    chainId,
                    blockHash,
                    blockHeight,
                    blockTime,
                    crossChainType,
                    transferType,
                    fromChainId,
                    toChainId,
                    transferTokenSymbol,
                    transferAmount,
                    transferTime,
                    transferTransactionId,
                    fromAddress,
                    toAddress,
                    receiveTokenSymbol,
                    receiveAmount,
                    receiveTime,
                    receiveTransactionId,
                    receiptId
            }
        }",
            Variables = new
            {
                chainId = chainId,
                transactionId = transactionId
            }
        };
    }

    private async Task<T> QueryDataAsync<T>(GraphQLRequest request)
    {
        var data = await _graphQlClient.SendQueryAsync<T>(request);
        if (data.Errors == null || data.Errors.Length == 0)
        {
            return data.Data;
        }

        Log.Error("Query indexer failed. errors: {Errors}",
            string.Join(",", data.Errors.Select(e => e.Message).ToList()));
        return default;
    }
}

public class ConfirmedBlockHeight
{
    public SyncState SyncState { get; set; }
}

public class SyncState
{
    public long ConfirmedBlockHeight { get; set; }
}

public enum BlockFilterType
{
    BLOCK,
    TRANSACTION,
    LOG_EVENT
}