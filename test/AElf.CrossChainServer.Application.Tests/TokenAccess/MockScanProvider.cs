using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;

namespace AElf.CrossChainServer.TokenAccess;

public class MockScanProvider : IScanProvider
{
    private readonly Dictionary<string, TokenDetailDto> _tokenDetailMap = new Dictionary<string, TokenDetailDto>();
    private readonly Dictionary<string, IndexerTokenHolderInfoListDto> _tokenHolderMap = 
        new Dictionary<string, IndexerTokenHolderInfoListDto>();

    public void SetupTokenDetail(string symbol, TokenDetailDto tokenDetail)
    {
        _tokenDetailMap[symbol] = tokenDetail;
    }

    public void SetupTokenHolderList(string address, IndexerTokenHolderInfoListDto tokenHolders)
    {
        _tokenHolderMap[address] = tokenHolders;
    }

    public Task<IndexerTokenHolderInfoListDto> GetTokenHolderListAsync(string address, int skipCount, int maxResultCount, string symbol = "")
    {
        if (_tokenHolderMap.TryGetValue(address, out var holders))
        {
            // Filter by symbol if provided
            if (!string.IsNullOrEmpty(symbol))
            {
                var filteredItems = holders.Items.FindAll(h => h.Token?.Symbol == symbol);
                return Task.FromResult(new IndexerTokenHolderInfoListDto
                {
                    TotalCount = filteredItems.Count,
                    Items = filteredItems
                });
            }
            
            return Task.FromResult(holders);
        }
        
        // Return empty result if not found
        return Task.FromResult(new IndexerTokenHolderInfoListDto
        {
            TotalCount = 0,
            Items = new List<IndexerTokenHolderInfoDto>()
        });
    }

    public Task<TokenDetailDto> GetTokenDetailAsync(string symbol)
    {
        if (_tokenDetailMap.TryGetValue(symbol, out var tokenDetail))
        {
            return Task.FromResult(tokenDetail);
        }
        
        // Return a default token detail if not found
        return Task.FromResult(new TokenDetailDto
        {
            Token = new TokenBaseInfo
            {
                Symbol = symbol,
                Name = $"{symbol} Token",
                ImageUrl = $"https://example.com/{symbol}.png",
                Decimals = 8
            },
            TotalSupply = 1000000,
            MergeHolders = 10000,
            ChainIds = new List<string> { "AELF" }
        });
    }
}