using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;
using Nethereum.ABI.FunctionEncoding.Attributes;

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
        Task<FilterLogsDto> GetContractLogsAsync(string chainId, string contractAddress, long startHeight, long endHeight);
        Task<long> GetChainHeightAsync(string chainId);

        Task<FilterLogsAndEventsDto<TEventDto>> GetContractLogsAndParseAsync<TEventDto>(string chainId,
            string contractAddress, long startHeight, long endHeight, string logSignature)
            where TEventDto : IEventDTO, new();
    }
}