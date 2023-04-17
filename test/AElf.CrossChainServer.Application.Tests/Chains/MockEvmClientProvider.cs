using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;

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

    public Task<ChainStatusDto> GetChainStatusAsync(string chainId)
    {
        throw new System.NotImplementedException();
    }

    public Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
    {
        throw new System.NotImplementedException();
    }

    public Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId)
    {
        throw new System.NotImplementedException();
    }
}