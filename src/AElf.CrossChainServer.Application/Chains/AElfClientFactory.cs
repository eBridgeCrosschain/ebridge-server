using System.Collections.Concurrent;
using AElf.Client.Service;
using Microsoft.Extensions.Options;

namespace AElf.CrossChainServer.Chains
{
    public class AElfClientFactory : IBlockchainClientFactory<AElfClient>
    {
        private readonly ChainApiOptions _chainApiOptions;
        private readonly ConcurrentDictionary<string, AElfClient> _clientDic;

        public AElfClientFactory(IOptionsSnapshot<ChainApiOptions> apiOptions)
        {
            _chainApiOptions = apiOptions.Value;
            _clientDic = new ConcurrentDictionary<string, AElfClient>();
        }

        public AElfClient GetClient(string chainId)
        {
            if (_clientDic.TryGetValue(chainId, out var client))
            {
                return client;
            }

            client = new AElfClient(_chainApiOptions.ChainNodeApis[chainId]);
            _clientDic[chainId] = client;
            return client;
        }
    }
}