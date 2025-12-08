using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.Chains;

public class EvmClientFactoryTests : CrossChainServerApplicationTestBase
{
    private readonly IBlockchainClientFactory<Nethereum.Web3.Web3> _factory;

    public EvmClientFactoryTests()
    {
        _factory = GetRequiredService<IBlockchainClientFactory<Nethereum.Web3.Web3>>();
    }
    
    [Fact]
    public void GetClientTest()
    {
        var client = _factory.GetClient("Ethereum");
        client.ShouldNotBeNull();
    }
}