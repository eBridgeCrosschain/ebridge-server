using GraphQL.Client.Abstractions;

namespace AElf.CrossChainServer.Indexer;

public interface IGraphQLClientFactory
{
    IGraphQLClient GetClient(GraphQLClientEnum clientEnum);
}