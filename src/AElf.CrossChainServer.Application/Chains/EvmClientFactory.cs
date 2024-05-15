using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace AElf.CrossChainServer.Chains
{
    public class EvmClientFactory : IBlockchainClientFactory<Nethereum.Web3.Web3>
    {
        private readonly ChainApiOptions _chainApiOptions;
        private readonly ILogger<EvmClientFactory> _logger;

        public EvmClientFactory(IOptionsSnapshot<ChainApiOptions> apiOptions, ILogger<EvmClientFactory> logger)
        {
            _logger = logger;
            _chainApiOptions = apiOptions.Value;
        }

        public Nethereum.Web3.Web3 GetClient(string chainId)
        {
            _logger.LogInformation("Get chain api:{api}", _chainApiOptions.ChainNodeApis[chainId]);
            return new Nethereum.Web3.Web3(_chainApiOptions.ChainNodeApis[chainId]);
        }
    }
}