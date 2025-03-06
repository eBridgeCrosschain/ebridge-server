using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;
using Nethereum.ABI.FunctionEncoding.Attributes;

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
        return Task.FromResult(new ChainStatusDto
        {
            ChainId = chainId,
            BlockHeight = 100,
            ConfirmedBlockHeight = 90
        });
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
        return Task.FromResult(new MerklePathDto
        {
            MerklePathNodes = new List<MerklePathNodeDto>
            {
                new MerklePathNodeDto
                {
                    Hash = "Hash",
                    IsLeftChildNode = true
                }
            }
        });
    }

    public Task<FilterLogsDto> GetContractLogsAsync(string chainId, string contractAddress, long startHeight, long endHeight)
    {
        throw new NotImplementedException();
    }

    public Task<FilterLogsAndEventsDto<TEventDTO>> GetContractLogsAndParseAsync<TEventDTO>(string chainId, string contractAddress, long startHeight, long endHeight,
        string logSignature) where TEventDTO : IEventDTO, new()
    {
        throw new NotImplementedException();
    }
}