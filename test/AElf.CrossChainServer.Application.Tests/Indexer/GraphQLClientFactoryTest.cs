using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.Indexer;

public class GraphQLClientFactoryTest : CrossChainServerApplicationTestBase
{
    private readonly IGraphQLClientFactory _graphQlClientFactory;

    public GraphQLClientFactoryTest()
    {
        _graphQlClientFactory = GetRequiredService<IGraphQLClientFactory>();
    }

    [Fact]
    public void GetClientTest()
    {
        var client = _graphQlClientFactory.GetClient(GraphQLClientEnum.CrossChainClient);
        client.ShouldNotBeNull();
    }
}