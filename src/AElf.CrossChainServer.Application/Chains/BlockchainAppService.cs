using System;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;
using AElf.ExceptionHandler;
using Volo.Abp;

namespace AElf.CrossChainServer.Chains
{
    [RemoteService(IsEnabled = false)]
    public class BlockchainAppService : CrossChainServerAppService, IBlockchainAppService
    {
        private readonly IBlockchainClientProviderFactory _blockchainClientProviderFactory;

        public BlockchainAppService(IBlockchainClientProviderFactory blockchainClientProviderFactory)
        {
            _blockchainClientProviderFactory = blockchainClientProviderFactory;
        }

        public async Task<TokenDto> GetTokenInfoAsync(string chainId, string address, string symbol)
        {
            var provider = await _blockchainClientProviderFactory.GetBlockChainClientProviderAsync(chainId);
            if(provider == null)
            {
                return null;
            }

            return await provider.GetTokenAsync(chainId, address, symbol);
        }
        
        public async Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false)
        {
            var provider = await _blockchainClientProviderFactory.GetBlockChainClientProviderAsync(chainId);
            if(provider == null)
            {
                return null;
            }
            
            return await provider.GetBlockByHeightAsync(chainId, height,includeTransactions);
        }

        [ExceptionHandler(typeof(Exception),Message = "[Bridge chain] Get chain status failed.",
            ReturnDefault = ReturnDefault.New, LogTargets = new[] {"chainId"})]
        public virtual async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
        {
            var provider = await _blockchainClientProviderFactory.GetBlockChainClientProviderAsync(chainId);
            if(provider == null)
            {
                return null;
            }
            return await provider.GetChainStatusAsync(chainId);
        }
        [ExceptionHandler(typeof(Exception),Message = "[Bridge chain] Get transaction result failed.",
            ReturnDefault = ReturnDefault.New, LogTargets = new[] {"chainId","transactionId"})]
        public async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
        {
            var provider = await _blockchainClientProviderFactory.GetBlockChainClientProviderAsync(chainId);
            if(provider == null)
            {
                return null;
            }
            return await provider.GetTransactionResultAsync(chainId, transactionId);
        }

        public async Task<MerklePathDto> GetMerklePathAsync(string chainId, string transactionId)
        {
            var provider = await _blockchainClientProviderFactory.GetBlockChainClientProviderAsync(chainId);
            if(provider == null)
            {
                return null;
            }
            return await provider.GetMerklePathAsync(chainId, transactionId);
        }
    }
}