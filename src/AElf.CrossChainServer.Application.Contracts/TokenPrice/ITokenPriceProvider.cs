using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenPrice;

public interface ITokenPriceProvider
{
    Task<decimal> GetPriceAsync(string pair);
    Task<decimal> GetHistoryPriceAsync(string pair, string dateTime);
}