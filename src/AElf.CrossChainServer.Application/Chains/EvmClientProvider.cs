using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;
using Microsoft.Extensions.Options;
using Nethereum.BlockchainProcessing.BlockStorage.Entities.Mapping;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.RPC.Eth.DTOs;
using Serilog;

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

        public async Task<FilterLogsDto> GetContractLogsAsync(string chainId, string contractAddress, long startHeight,
            long endHeight)
        {
            Log.Debug("Get contract logs from {startHeight} to {endHeight}", startHeight, endHeight);
            var client = BlockchainClientFactory.GetClient(chainId);
            var filterLogs = await client.Eth.Filters.GetLogs.SendRequestAsync(new NewFilterInput
            {
                Address = new[] { contractAddress },
                FromBlock = new BlockParameter((ulong)startHeight),
                ToBlock = new BlockParameter((ulong)endHeight)
            });
            var logs = filterLogs.Select(l => new FilterLog
            {
                Address = l.Address,
                BlockHash = l.BlockHash,
                BlockNumber = l.BlockNumber.ToLong(),
                Data = l.Data,
                LogIndex = l.LogIndex.ToLong(),
                Topics = l.Topics,
                TransactionHash = l.TransactionHash,
                TransactionIndex = l.TransactionIndex.ToLong(),
                Type = l.Type,
                Removed = l.Removed
            }).ToList();
            return new FilterLogsDto
            {
                Logs = logs
            };
        }
    }
}