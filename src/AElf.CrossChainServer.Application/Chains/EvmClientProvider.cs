using System;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.ExceptionHandler;
using AElf.CrossChainServer.Tokens;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Options;
using Nethereum.BlockchainProcessing.BlockStorage.Entities.Mapping;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;

namespace AElf.CrossChainServer.Chains
{
    public class EvmClientProvider : IBlockchainClientProvider
    {
        protected readonly IBlockchainClientFactory<Nethereum.Web3.Web3> BlockchainClientFactory;
        public IOptionsSnapshot<BlockConfirmationOptions> BlockConfirmationOptions { get; set; }

        public EvmClientProvider(IBlockchainClientFactory<Nethereum.Web3.Web3> blockchainClientFactory)
        {
            BlockchainClientFactory = blockchainClientFactory;
        }

        public BlockchainType ChainType { get; } = BlockchainType.Evm;

        public async Task<TokenDto> GetTokenAsync(string chainId, string address, string symbol)
        {
            var client = BlockchainClientFactory.GetClient(chainId);
            var contractHandler = client.Eth.GetContractHandler(address);
            
            return new TokenDto
            {
                ChainId = chainId,
                Address = address,
                Decimals = await contractHandler.QueryAsync<DecimalsFunction, int>(),
                Symbol = await contractHandler.QueryAsync<SymbolFunction, string>()
            };
        }
        [ExceptionHandler(typeof(Exception),Message = "Get evm block height failed.",
            TargetType = typeof(ExceptionHandlingService),
            MethodName = nameof(ExceptionHandlingService.ThrowException))]
        public Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false)
        {
            throw new NotImplementedException();
        }

        public async Task<long> GetChainHeightAsync(string chainId)
        {
            var client = BlockchainClientFactory.GetClient(chainId);
            var latestBlockNumber = await client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            return latestBlockNumber.ToLong();
        }
        
        [ExceptionHandler(typeof(Exception),Message = "Get evm chain status failed.",
            TargetType = typeof(ExceptionHandlingService),
            MethodName = nameof(ExceptionHandlingService.ThrowException))]
        public async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
        {
            var client = BlockchainClientFactory.GetClient(chainId);
            var latestBlockNumber = await client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var blockNumber = latestBlockNumber.ToLong();
            
            return new ChainStatusDto
            {
                ChainId = chainId,
                BlockHeight = blockNumber,
                ConfirmedBlockHeight = blockNumber - BlockConfirmationOptions.Value.ConfirmationCount[chainId]
            };
        }
        
        public Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
        {
            throw new NotImplementedException();
        }

        public Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId)
        {
            throw new NotImplementedException();
        }
    }
}