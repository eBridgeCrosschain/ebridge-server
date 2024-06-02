using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.Chains;

public class TronClientFactoryTests : CrossChainServerApplicationTestBase
{
    private readonly IBlockchainClientFactory<TronClient.TronClient> _factory;

    public TronClientFactoryTests()
    {
        _factory = GetRequiredService<IBlockchainClientFactory<TronClient.TronClient>>();
    }
    
    [Fact]
    public void GetClientTest()
    {
        var client = _factory.GetClient("Tron");
        client.ShouldNotBeNull();
    }
}