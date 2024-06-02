using System;
using System.Numerics;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Chains.TronFunctionMessage;
using AElf.CrossChainServer.Tokens;
using Microsoft.Extensions.Options;
using TronClient;

namespace AElf.CrossChainServer.Chains
{
    public class TronClientProvider : IBlockchainClientProvider
    {
        protected readonly IBlockchainClientFactory<TronClient.TronClient> BlockchainClientFactory;
        public IOptionsSnapshot<BlockConfirmationOptions> BlockConfirmationOptions { get; set; }

        public TronClientProvider(IBlockchainClientFactory<TronClient.TronClient> blockchainClientFactory)
        {
            BlockchainClientFactory = blockchainClientFactory;
        }

        public BlockchainType ChainType { get; } = BlockchainType.Tron;

        public async Task<TokenDto> GetTokenAsync(string chainId, string address, string symbol)
        {
            var client = BlockchainClientFactory.GetClient(chainId);
            var contractHandler = client.GetContract(address);
            
            var decimals = await GetDecimals(contractHandler);
            symbol = await GetSymbol(contractHandler);

            return new TokenDto
            {
                ChainId = chainId,
                Address = address,
                Decimals = (int)decimals,
                Symbol = symbol
            };
        }

        public Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false)
        {
            throw new NotImplementedException();
        }

        public async Task<long> GetChainHeightAsync(string chainId)
        {
            var client = BlockchainClientFactory.GetClient(chainId);
            var latestBlock = await client.GetNowBlockAsync();
            return latestBlock.block_header.raw_data.number;
        }
        
        public async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
        {
            var client = BlockchainClientFactory.GetClient(chainId);
            var latestBlock = await client.GetNowBlockAsync();
            var blockNumber = latestBlock.block_header.raw_data.number;
            
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
        
        private static async Task<string> GetSymbol(IContract contractHandler)
        {
            var result = await contractHandler.CallAsync<SymbolFunctionMessage, SymbolDto>(new TronConstantContractFunctionMessage<SymbolFunctionMessage>
            {
                FunctionMessage = new SymbolFunctionMessage()
            });
            return result.Symbol;
        }

        private static async Task<BigInteger> GetDecimals(IContract contractHandler)
        {
            var result = await contractHandler.CallAsync<DecimalsFunctionMessage, DecimalsDto>(new TronConstantContractFunctionMessage<DecimalsFunctionMessage>
            {
                FunctionMessage = new DecimalsFunctionMessage()
            });
            return result.Decimals;
        }
    }
}