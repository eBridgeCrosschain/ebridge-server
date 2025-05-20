using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Chains;

public class MockEvmClientProvider : IBlockchainClientProvider
{
    public BlockchainType ChainType { get; } = BlockchainType.Evm;

    public async Task<TokenDto> GetTokenAsync(string chainId, string address, string symbol)
    {
        return new TokenDto
        {
            ChainId = chainId,
            Address = address,
            Symbol = "MockSymbol"
        };
    }

    public Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false)
    {
        throw new System.NotImplementedException();
    }

    public Task<long> GetChainHeightAsync(string chainId)
    {
        throw new System.NotImplementedException();
    }

    public async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
    {
        return new ChainStatusDto
        {
            ChainId = chainId,
            BlockHeight = 105,
            ConfirmedBlockHeight = 100
        };
    }

    public Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
    {
        return Task.FromResult(new TransactionResultDto
        {
            ChainId = chainId,
            BlockHeight = 100,
            Transaction = new TransactionDto(),
            BlockHash = "BlockHash",
            IsFailed = false,
            IsMined = true
        });
    }

    public Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId)
    {
        throw new System.NotImplementedException();
    }

    public Task<FilterLogsDto> GetContractLogsAsync(string chainId, string contractAddress, long startHeight, long endHeight)
    {
        throw new System.NotImplementedException();
    }

    public Task<FilterLogsAndEventsDto<TEventDTO>> GetContractLogsAndParseAsync<TEventDTO>(string chainId, string contractAddress, long startHeight, long endHeight,
        string logSignature) where TEventDTO : IEventDTO, new()
    {
        throw new System.NotImplementedException();
    }
}