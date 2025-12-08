using System.Threading.Tasks;

namespace AElf.CrossChainServer.Contracts.Bridge
{
    public interface IBridgeContractProviderFactory
    {
        Task<IBridgeContractProvider> GetBridgeContractProviderAsync(string chainId);
    }
}