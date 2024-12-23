using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Chains.Ton;
using AElf.CrossChainServer.Contracts.Bridge;
using AElf.CrossChainServer.Tokens;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace AElf.CrossChainServer.Chains
{
    [RemoteService(IsEnabled = false)]
    public class BlockchainAppService : CrossChainServerAppService, IBlockchainAppService
    {
        private readonly IBlockchainClientProviderFactory _blockchainClientProviderFactory;
        private readonly ITonIndexProvider _tonIndexProvider;
        private BridgeContractOptions _bridgeContractOptions;

        public BlockchainAppService(IBlockchainClientProviderFactory blockchainClientProviderFactory,
            ITonIndexProvider tonIndexProvider, IOptionsSnapshot<BridgeContractOptions> bridgeContractOptions)
        {
            _blockchainClientProviderFactory = blockchainClientProviderFactory;
            _tonIndexProvider = tonIndexProvider;
            _bridgeContractOptions = bridgeContractOptions.Value;
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
        
        public async Task<long> GetChainHeightAsync(string chainId)
        {
            var provider = await _blockchainClientProviderFactory.GetBlockChainClientProviderAsync(chainId);
            if(provider == null)
            {
                return 0;
            }
            
            return await provider.GetChainHeightAsync(chainId);
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

        public Task<List<TonTransactionDto>> GetTonTransactionAsync(GetTonTransactionInput input)
        {
            return _tonIndexProvider.GetTonTransactionAsync(input);
        }
        
        public Task<string> GetTonUserFriendlyAddressAsync(string chainId, string address)
        {
            return _tonIndexProvider.GetTonUserFriendlyAddressAsync(chainId, address);
        }

        public async Task<FilterLogsDto> GetContractLogsAsync(string chainId, string contractAddress, long startHeight, long endHeight)
        {
            var provider = await _blockchainClientProviderFactory.GetBlockChainClientProviderAsync(chainId);
            if(provider == null)
            {
                return null;
            }
            return await provider.GetContractLogsAsync(chainId, contractAddress, startHeight, endHeight);
        }
    }
}