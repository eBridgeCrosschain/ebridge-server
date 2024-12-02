using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;

namespace AElf.CrossChainServer.TokenAccess;

public interface ISymbolMarketProvider
{
    Task IssueTokenAsync(IssueTokenInput input);
    Task<List<string>> GetIssueChainListAsync(string symbol);
}

public class SymbolMarketProvider : ISymbolMarketProvider
{
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly IHttpProvider _httpProvider;
    
    public Task IssueTokenAsync(IssueTokenInput input)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<string>> GetIssueChainListAsync(string symbol)
    {
        throw new System.NotImplementedException();
    }
}