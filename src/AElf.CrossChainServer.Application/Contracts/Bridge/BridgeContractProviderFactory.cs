using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.Contracts.Bridge;

public class BridgeContractProviderFactory : IBridgeContractProviderFactory, ITransientDependency
{
    private readonly IEnumerable<IBridgeContractProvider> _blockchainClientProviders;
    private readonly IChainAppService _chainAppService;

    public BridgeContractProviderFactory(IEnumerable<IBridgeContractProvider> blockchainClientProviders,
        IChainAppService chainAppService)
    {
        _blockchainClientProviders = blockchainClientProviders;
        _chainAppService = chainAppService;
    }

    public async Task<IBridgeContractProvider> GetBridgeContractProviderAsync(string chainId)
    {
        var chain = await _chainAppService.GetAsync(chainId);
        return chain == null ? null : _blockchainClientProviders.First(o => o.ChainType == chain.Type);
    }
}