using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;

namespace AElf.CrossChainServer.TokenAccess;

public interface ILiquidityDataProvider
{
    Task<string> GetTokenTvlAsync(string symbol);
}

public class LiquidityDataProvider : ILiquidityDataProvider
{
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly IHttpProvider _httpProvider;
    public Task<string> GetTokenTvlAsync(string symbol)
    {
        throw new System.NotImplementedException();
    }
}