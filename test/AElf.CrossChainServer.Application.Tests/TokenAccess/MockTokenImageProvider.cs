using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenAccess;

public class MockTokenImageProvider : ITokenImageProvider
{
    public async Task<string> GetTokenImageAsync(string symbol)
    {
        return "https://example.com/image.png";
    }
}