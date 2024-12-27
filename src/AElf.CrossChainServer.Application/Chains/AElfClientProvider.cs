using System;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.CrossChainServer.Tokens;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using GetTokenInfoInput = AElf.Client.MultiToken.GetTokenInfoInput;

namespace AElf.CrossChainServer.Chains
{
    public class AElfClientProvider : IBlockchainClientProvider
    {
        protected readonly IBlockchainClientFactory<AElfClient> BlockchainClientFactory;
        private readonly AccountOptions _accountOptions;

        public AElfClientProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
            IOptionsSnapshot<AccountOptions> accountOptions)
        {
            BlockchainClientFactory = blockchainClientFactory;
            _accountOptions = accountOptions.Value;
        }

        public BlockchainType ChainType { get; } = BlockchainType.AElf;

        public async Task<TokenDto> GetTokenAsync(string chainId, string address, string symbol)
        {
            var client = BlockchainClientFactory.GetClient(chainId);
            if (address.IsNullOrWhiteSpace())
            {
                address = (await client.GetContractAddressByNameAsync(
                    HashHelper.ComputeFrom("AElf.ContractNames.Token"))).ToBase58();
            }
            
            var token = await GetTokenInfoAsync(chainId, address, symbol);
            if (token == null)
            {
                return null;
            }

            return new TokenDto
            {
                ChainId = chainId,
                Address = address,
                Decimals = token.Decimals,
                Symbol = token.Symbol,
                IssueChainId = token.IssueChainId
            };
        }
        
        public virtual async Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false)
        {
            var client = BlockchainClientFactory.GetClient(chainId);
            var block = await client.GetBlockByHeightAsync(height,includeTransactions);

            return new BlockDto
            {
                BlockHash = block.BlockHash,
                Header = new BlockHeaderDto
                {
                    Height = block.Header.Height,
                    Time = block.Header.Time,
                    ChainId = block.Header.ChainId,
                    PreviousBlockHash = block.Header.PreviousBlockHash
                },
                Body = new BlockBodyDto
                {
                    Transactions = block.Body.Transactions,
                    TransactionsCount = block.Body.TransactionsCount
                }
            };
        }

        public Task<long> GetChainHeightAsync(string chainId)
        {
            throw new NotImplementedException();
        }
        
        public virtual async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
        {
            var client = BlockchainClientFactory.GetClient(chainId);
            var status = await client.GetChainStatusAsync();
            return new ChainStatusDto
            {
                ChainId = chainId,
                BlockHeight = status.BestChainHeight,
                ConfirmedBlockHeight = status.LastIrreversibleBlockHeight
            };
        }
        
        public async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
        {
            var client = BlockchainClientFactory.GetClient(chainId);
            var result = await client.GetTransactionResultAsync(transactionId);
            
            return new TransactionResultDto
            {
                ChainId = chainId,
                IsMined = result.Status == TransactionResultStatus.Mined.ToString().ToUpper(),
                IsFailed = result.Status != TransactionResultStatus.Mined.ToString().ToUpper() &&
                            result.Status != TransactionResultStatus.Pending.ToString().ToUpper(),
                BlockHeight = result.BlockNumber,
                BlockHash = result.BlockHash,
                Transaction = result.Transaction,
                Logs = result.Logs
            };
        }

        public async Task<AElf.Contracts.MultiToken.TokenInfo> GetTokenInfoAsync(string chainId, string address, string symbol)
        {
            var client = BlockchainClientFactory.GetClient(chainId);

            var paramGetBalance = new GetTokenInfoInput
            {
                Symbol = symbol
            };
            var transactionGetToken =
                await client.GenerateTransactionAsync(client.GetAddressFromPrivateKey(GetPrivateKey(chainId)), address,
                    "GetTokenInfo",
                    paramGetBalance);
            var txWithSignGetToken = client.SignTransaction(GetPrivateKey(chainId), transactionGetToken);
            var transactionGetTokenResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSignGetToken.ToByteArray().ToHex()
            });
            var token =
                AElf.Contracts.MultiToken.TokenInfo.Parser.ParseFrom(
                    ByteArrayHelper.HexStringToByteArray(transactionGetTokenResult));

            if (token.Symbol != symbol)
            {
                return null;
            }

            return token;
        }
        
        public async Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId)
        {
            var client = BlockchainClientFactory.GetClient(chainId);
            return await client.GetMerklePathByTransactionIdAsync(txId);
        }

        public Task<FilterLogsDto> GetContractLogsAsync(string chainId, string contractAddress, long startHeight, long endHeight)
        {
            throw new NotImplementedException();
        }

        protected string GetPrivateKey(string chainId)
        {
            return _accountOptions.PrivateKeys[chainId];
        }
    }
}