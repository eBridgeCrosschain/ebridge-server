using System.Threading.Tasks;

namespace AElf.CrossChainServer.CrossChain;

public class MockAetherLinkProvider : IAetherLinkProvider
{
    public async Task<int> CalculateCrossChainProgressAsync(AetherLinkCrossChainStatusInput input)
    {
        return 50;
    }
}