using System.Threading.Tasks;

namespace AElf.CrossChainServer.Chains
{
    public interface IBlockchainClientProviderFactory
    {
        Task<IBlockchainClientProvider> GetBlockChainClientProviderAsync(string chainId);
    }
}