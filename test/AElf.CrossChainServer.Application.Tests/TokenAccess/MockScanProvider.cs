using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;

namespace AElf.CrossChainServer.TokenAccess;

public class MockScanProvider : IScanProvider
{
    public async Task<IndexerTokenHolderInfoListDto> GetTokenHolderListAsync(string address, int skipCount, int maxResultCount, string symbol = "")
    {
        return new IndexerTokenHolderInfoListDto
        {
            TotalCount = 0,
            Items = new List<IndexerTokenHolderInfoDto>()
        };
    }

    public async Task<TokenDetailDto> GetTokenDetailAsync(string symbol)
    {
        return new TokenDetailDto
        {
            Token = new TokenBaseInfo
            {
                Name = symbol,
                Symbol = symbol,
                ImageUrl = "https://example.com/image.png",
                Decimals = 8
            },
            TotalSupply = 1000,
            MergeHolders = 1000,
            ChainIds = new List<string> { "AELF" }
        };
    }
}