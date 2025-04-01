using System;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;

namespace AElf.CrossChainServer.Chains;

public class SolanaClientProvider : IBlockchainClientProvider
{
    protected readonly ISolanaIndexClientProvider _indexClientProvider;

    public SolanaClientProvider(ISolanaIndexClientProvider indexClientProvider)
    {
        _indexClientProvider = indexClientProvider;
    }

    public BlockchainType ChainType { get; } = BlockchainType.Svm;

    public async Task<TokenDto> GetTokenAsync(string chainId, string address, string symbol)
    {
        var response = await _indexClientProvider.GetClient(chainId).GetTokenMintInfoAsync(address);
        var decimals = response?.Result?.Value?.Data?.Parsed?.Info?.Decimals;
        return new TokenDto
        {
            Id = address.IsNullOrEmpty() ? Guid.Empty : GuidHelper.UniqGuid(address),
            ChainId = chainId,
            Address = address,
            Decimals = int.Parse(decimals.HasValue ? decimals.ToString() : "0"),
            Symbol = symbol
        };
    }

    public Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false)
    {
        throw new NotImplementedException();
    }

    public async Task<long> GetChainHeightAsync(string chainId)
    {
        throw new NotImplementedException();
    }

    public async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
    {
        throw new NotImplementedException();
    }

    public Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
    {
        throw new NotImplementedException();
    }

    public Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId)
    {
        throw new NotImplementedException();
    }

    public Task<FilterLogsDto> GetContractLogsAsync(string chainId, string contractAddress, long startHeight,
        long endHeight)
    {
        throw new NotImplementedException();
    }
}
