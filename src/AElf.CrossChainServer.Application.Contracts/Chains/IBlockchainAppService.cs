using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;
using Solnet.Rpc.Models;

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
        Task<string> GetTonUserFriendlyAddressAsync(string chainId, string address);
        Task<List<string>> GetSignaturesForAddressAsync(string chainId, string accountPubKey, ulong limit = 1000, 
            string before = null, string until = null);
        Task<TransactionMetaSlotInfo> GetSolanaTransactionAsync(string chainId, string signature);
        Task<BlockInfo> GetSolanaBlockAsync(string chainId, ulong slot);
        Task<FilterLogsDto> GetContractLogsAsync(string chainId, string contractAddress, long startHeight, long endHeight);
        Task<long> GetChainHeightAsync(string chainId);
    }
}