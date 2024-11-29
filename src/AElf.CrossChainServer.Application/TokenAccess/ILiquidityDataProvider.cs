using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenAccess;

public interface ILiquidityDataProvider
{
    Task<string> GetTokenTvlAsync(string symbol);
}

public class LiquidityDataProvider : ILiquidityDataProvider
{
    public Task<string> GetTokenTvlAsync(string symbol)
    {
        throw new System.NotImplementedException();
    }
}