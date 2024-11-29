using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenAccess;

public interface ISymbolMarketProvider
{
    Task IssueTokenAsync(IssueTokenInput input);
    Task<List<string>> GetIssueChainListAsync(string symbol);
}

public class SymbolMarketProvider : ISymbolMarketProvider
{
    public Task IssueTokenAsync(IssueTokenInput input)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<string>> GetIssueChainListAsync(string symbol)
    {
        throw new System.NotImplementedException();
    }
}