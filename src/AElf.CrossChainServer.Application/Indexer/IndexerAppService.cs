using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.ExceptionHandler;
using AElf.CrossChainServer.HttpClient;
using AElf.ExceptionHandler;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;

namespace AElf.CrossChainServer.Indexer;

[RemoteService(IsEnabled = false)]
public class IndexerAppService : CrossChainServerAppService, IIndexerAppService
{
    private readonly IGraphQLClient _graphQlClient;
    private readonly IChainAppService _chainAppService;
    private readonly IHttpProvider _httpProvider;
    private readonly SyncStateServiceOption _syncStateServiceOption;
    private ApiInfo _syncStateUri => new(HttpMethod.Get, _syncStateServiceOption.SyncStateUri);

    public IndexerAppService(IGraphQLClientFactory graphQlClientFactory, IChainAppService chainAppService,
        IHttpProvider httpProvider,
        IOptionsSnapshot<SyncStateServiceOption> syncStateServiceOption)
    {
        _graphQlClient = graphQlClientFactory.GetClient(GraphQLClientEnum.CrossChainClient);
        _chainAppService = chainAppService;
        _httpProvider = httpProvider;
        _syncStateServiceOption = syncStateServiceOption.Value;
    }

    [ExceptionHandler(typeof(Exception), Message = "Query swap syncState error",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionReturnLong))]
    public virtual async Task<long> GetLatestIndexHeightAsync(string chainId)
    {
        var chain = await _chainAppService.GetAsync(chainId);
        if (chain == null)
        {
            return 0;
        }

        var aelfChainId = ChainHelper.ConvertChainIdToBase58(chain.AElfChainId);
        var res = await _httpProvider.InvokeAsync<SyncStateResponse>(_syncStateServiceOption.BaseUrl, _syncStateUri);
        var blockHeight = res.CurrentVersion.Items.FirstOrDefault(i => i.ChainId == aelfChainId)
            ?.LastIrreversibleBlockHeight;
        Logger.LogInformation("Get latest index height. chainId: {chainId}, height: {height}", aelfChainId,
            blockHeight);
        return blockHeight ?? 0;
    }

    public async Task<(bool, CrossChainTransferInfoDto)> GetPendingTransactionAsync(string chainId,
        string transferTransactionId)
    {
        var data = await QueryDataAsync<GraphQLResponse<CrossChainTransferInfoDto>>(GetRequest(chainId,
            transferTransactionId));
        if (data == null)
        {
            Logger.LogInformation(
                "Get pending transaction failed. chainId: {chainId}, transferTransactionId: {transferTransactionId}",
                chainId, transferTransactionId);
            return (false, null);
        }

        Log.ForContext("chainId", chainId).Information(
            "Get pending transaction success. chainId: {chainId}, transferTransactionId: {transferTransactionId}, data: {data}",
            chainId, transferTransactionId, JsonConvert.SerializeObject(data.Data));
        return (true, data.Data);
    }

    public async Task<(bool, CrossChainTransferInfoDto)> GetPendingReceiveTransactionAsync(string chainId,
        string receiveTransactionId)
    {
        var data = await QueryDataAsync<GraphQLResponse<CrossChainTransferInfoDto>>(GetPendingReceiveTransactionRequest(
            chainId,
            receiveTransactionId));
        if (data == null)
        {
            Log.Debug(
                "Get pending receive transaction failed. chainId: {chainId}, receiveTransactionId: {receiveTransactionId}",
                chainId, receiveTransactionId);
            return (false, null);

        }

        Log.ForContext("chainId", chainId).Information(
            "Get pending receive transaction success. chainId: {chainId}, receiveTransactionId: {receiveTransactionId}, data: {data}",
            chainId, receiveTransactionId, JsonConvert.SerializeObject(data.Data));
        return (true, data.Data);
    }

    public async Task<(bool, CrossChainTransferInfoDto)> GetPendingReceiptAsync(string chainId, string receiptId)
    {
        var data = await QueryDataAsync<GraphQLResponse<CrossChainTransferInfoDto>>(GetPendingReceiptRequest(chainId,
            receiptId));
        if (data == null)
        {
            Log.Debug("Get pending receipt failed. chainId: {chainId}, receiptId: {receiptId}",
                chainId, receiptId);
            return (false, null);
        }

        Log.ForContext("chainId", chainId).Information(
            "Get pending receipt success. chainId: {chainId}, receiptId: {receiptId}, data: {data}",
            chainId, receiptId, JsonConvert.SerializeObject(data.Data));
        return (true, data.Data);
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
                    receiptId,
                    receiveBlockHeight
            }
        }",
            Variables = new
            {
                chainId = chainId,
                transactionId = transactionId
            }
        };
    }

    private GraphQLRequest GetPendingReceiveTransactionRequest(string chainId, string transactionId)
    {
        return new GraphQLRequest
        {
            Query =
                @"query(
                    $chainId:String,
                    $transactionId:String
                ) {
                    data:homogeneousCrossChainReceiveInfo(
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
                    receiptId,
                    receiveBlockHeight
            }
        }",
            Variables = new
            {
                chainId = chainId,
                transactionId = transactionId
            }
        };
    }


    private GraphQLRequest GetPendingReceiptRequest(string chainId, string receiptId)
    {
        return new GraphQLRequest
        {
            Query =
                @"query(
                    $chainId:String,
                    $receiptId:String
                ) {
                    data:queryCrossChainTransferInfoByReceiptId(
                        input: {
                            chainId:$chainId,
                            receiptId:$receiptId
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
                    receiptId,
                    receiveBlockHeight
            }
        }",
            Variables = new
            {
                chainId = chainId,
                receiptId = receiptId
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