using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.Chains
{
    public class BlockchainClientProviderFactory : IBlockchainClientProviderFactory, ITransientDependency
    {
        private readonly IEnumerable<IBlockchainClientProvider> _blockchainClientProviders;
        private readonly IChainAppService _chainAppService;

        public BlockchainClientProviderFactory(IEnumerable<IBlockchainClientProvider> blockchainClientProviders,
            IChainAppService chainAppService)
        {
            _blockchainClientProviders = blockchainClientProviders;
            _chainAppService = chainAppService;
        }

        public async Task<IBlockchainClientProvider> GetBlockChainClientProviderAsync(string chainId)
        {
            var chain = await _chainAppService.GetAsync(chainId);
            return chain == null ? null : _blockchainClientProviders.First(o => o.ChainType == chain.Type);
        }
    }
}