using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace AElf.CrossChainServer.Indexer;

[RemoteService(IsEnabled = false)]
public class IndexerAppService: CrossChainServerAppService, IIndexerAppService
{
    private readonly IGraphQLClient _graphQlClient;
    private readonly IChainAppService _chainAppService;

    public IndexerAppService(IGraphQLClientFactory graphQlClientFactory, IChainAppService chainAppService)
    {
        _graphQlClient = graphQlClientFactory.GetClient(GraphQLClientEnum.CrossChainServerClient);
        _chainAppService = chainAppService;
    }

    public async Task<long> GetLatestIndexHeightAsync(string chainId)
    {
        var chain = await _chainAppService.GetAsync(chainId);
        if (chain == null)
        {
            return 0;
        }
        var data = await QueryDataAsync<ConfirmedBlockHeight>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$filterType:BlockFilterType!) {
                    syncState(dto: {chainId:$chainId,filterType:$filterType}){
                        confirmedBlockHeight}
                    }",
            Variables = new
            {
                chainId = ChainHelper.ConvertChainIdToBase58(chain.AElfChainId),
                filterType = BlockFilterType.LOG_EVENT
            }
        });

        return data.SyncState.ConfirmedBlockHeight;
    }

    public async Task<CrossChainTransferDto> GetPendingTransactionAsync(string chainId,string transferTransactionId)
    {
        var data = await QueryDataAsync<CrossChainTransferDto>(GetCrossChainTransferRequest(chainId, transferTransactionId));
        if (data != null && !string.IsNullOrWhiteSpace(data.ReceiveTransactionId))
        {
            return data;
        }
        return null;
    }
    
    private GraphQLRequest GetCrossChainTransferRequest(string chainId, string transferTransactionId)
    {
        return new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$transferTransactionId:String){
            homogeneousCrossChainTransferInfo(input: {chainId:$chainId,transactionId:$transferTransactionId}){
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
                transferTransactionId = transferTransactionId
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

        Logger.LogError("Query indexer failed. errors: {Errors}",
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