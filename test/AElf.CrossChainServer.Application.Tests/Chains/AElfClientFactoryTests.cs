using AElf.Client.Service;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.Chains;

public class AElfClientFactoryTests : CrossChainServerApplicationTestBase
{
    private readonly IBlockchainClientFactory<AElfClient> _aelfClientFactory;

    public AElfClientFactoryTests()
    {
        _aelfClientFactory = GetRequiredService<IBlockchainClientFactory<AElfClient>>();
    }
    
    [Fact]
    public void GetClientTest()
    {
        
        var client = _aelfClientFactory.GetClient("MainChain_AELF");
        client.ShouldNotBeNull();
    }
}