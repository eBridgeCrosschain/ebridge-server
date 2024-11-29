using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenAccess;

public interface IScanProvider
{
    Task<int> GetTokenHoldersAsync(string symbol);
    Task<List<AvailableTokenDto>> GetOwnTokensAsync(string address);
}

public class ScanProvider : IScanProvider
{
    public Task<int> GetTokenHoldersAsync(string symbol)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<AvailableTokenDto>> GetOwnTokensAsync(string address)
    {
        throw new System.NotImplementedException();
    }
}