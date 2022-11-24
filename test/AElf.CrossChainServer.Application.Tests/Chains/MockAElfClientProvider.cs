using System;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;

namespace AElf.CrossChainServer.Chains;

public class MockAElfClientProvider : IBlockchainClientProvider
{
    public BlockchainType ChainType { get; } = BlockchainType.AElf;

    public async Task<TokenDto> GetTokenAsync(string chainId, string address, string symbol)
    {
        return new TokenDto
        {
            ChainId = chainId,
            Address = "MockTokenAddress",
            Symbol = symbol
        };
    }

    public async Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false)
    {
        return new BlockDto
        {
            BlockHash = "BlockHash",
            Header = new BlockHeaderDto
            {
                Height = height,
                Time = DateTime.UtcNow.AddMinutes(-3).AddSeconds(height),
                ChainId = chainId,
                PreviousBlockHash = "PreviousBlockHash"
            }
        };
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
        throw new NotImplementedException();
    }
}