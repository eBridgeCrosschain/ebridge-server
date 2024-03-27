using Microsoft.Extensions.Options;

namespace AElf.CrossChainServer.Chains
{
    public class TronClientFactory : IBlockchainClientFactory<TronClient.TronClient>
    {
        private readonly ChainApiOptions _chainApiOptions;

        public TronClientFactory(IOptionsSnapshot<ChainApiOptions> apiOptions)
        {
            _chainApiOptions = apiOptions.Value;
        }

        public TronClient.TronClient GetClient(string chainId)
        {
            return new TronClient.TronClient(_chainApiOptions.ChainNodeApis[chainId], _chainApiOptions.ApiKeys[chainId]);
        }
    }
}