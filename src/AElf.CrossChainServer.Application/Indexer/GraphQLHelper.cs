using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using Serilog;

namespace AElf.CrossChainServer.Indexer;

public interface IGraphQLHelper
{
    Task<T> QueryAsync<T>(GraphQLRequest request);
}

public class GraphQLHelper : IGraphQLHelper
{
    public const int PageCount = 1000;

    private readonly IGraphQLClient _graphQLClient;

    public GraphQLHelper(IGraphQLClient graphQLClient)
    {
        _graphQLClient = graphQLClient;
    }

    public async Task<T> QueryAsync<T>(GraphQLRequest request)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<T>(request);
        if (graphQlResponse.Errors is not { Length: > 0 })
        {
            return graphQlResponse.Data;
        }

        Log.Error("query graphQL err, errors = {Errors}",
            string.Join(",", graphQlResponse.Errors.Select(e => e.Message).ToList()));
       
        return default;
    }
}