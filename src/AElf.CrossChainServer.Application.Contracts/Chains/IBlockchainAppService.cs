using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;

namespace AElf.CrossChainServer.Chains
{
    public interface IBlockchainAppService
    {
        Task<TokenDto> GetTokenInfoAsync(string chainId, string address, string symbol = null);
        Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false);
        Task<ChainStatusDto> GetChainStatusAsync(string chainId);
        Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId);
        Task<MerklePathDto> GetMerklePathAsync(string chainId, string transactionId);
        Task<List<TonTransactionDto>> GetTonTransactionAsync(GetTonTransactionInput input);
    }
}