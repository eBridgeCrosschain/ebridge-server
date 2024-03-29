using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;

namespace AElf.CrossChainServer.Chains
{
    public interface IBlockchainClientProvider
    {
        BlockchainType ChainType { get; }
        Task<TokenDto> GetTokenAsync(string chainId, string address, string symbol);
        Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false);
        Task<long> GetChainHeightAsync(string chainId);
        Task<ChainStatusDto> GetChainStatusAsync(string chainId);
        Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId);
        Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId);
    }
}