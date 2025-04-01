using Microsoft.Extensions.Options;
using Solnet.Rpc;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.Chains;

public interface ISolanaIndexClientProvider
{
    IRpcClient GetClient(string chainId);
}

public class SolanaIndexClientProvider : ISolanaIndexClientProvider, ISingletonDependency
{
    private readonly ChainApiOptions _chainApiOptions;

    public SolanaIndexClientProvider(IOptionsSnapshot<ChainApiOptions> apiOptions)
    {
        _chainApiOptions = apiOptions.Value;
    }

    public IRpcClient GetClient(string chainId)
    {
        return ClientFactory.GetClient(_chainApiOptions.ChainNodeApis[chainId]);
    }
}