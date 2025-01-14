using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;
using AElf.CrossChainServer.Indexer;
using GraphQL;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.TokenAccess;

public interface IScanProvider
{
    Task<IndexerTokenHolderInfoListDto> GetTokenHolderListAsync(string address, int skipCount, int maxResultCount,
        string symbol = "");

    Task<TokenDetailDto> GetTokenDetailAsync(string symbol);
}

public class ScanProvider : IScanProvider, ITransientDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IHttpProvider _httpProvider;
    private readonly TokenAccessOptions _tokenAccessOptions;
    private ApiInfo ScanTokenDetailUri => new(HttpMethod.Get, _tokenAccessOptions.ScanTokenDetailUri);

    public ScanProvider(IGraphQLClientFactory graphQlClientFactory,
        IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions, IHttpProvider httpProvider)
    {
        _httpProvider = httpProvider;
        _graphQlHelper = new GraphQLHelper(graphQlClientFactory.GetClient(GraphQLClientEnum.ScanClient));
        _tokenAccessOptions = tokenAccessOptions.Value;
    }

    public async Task<IndexerTokenHolderInfoListDto> GetTokenHolderListAsync(string address, int skipCount,
        int maxResultCount, string symbol = "")
    {
        var indexerResult = await _graphQlHelper.QueryAsync<IndexerTokenHolderInfosDto>(new GraphQLRequest
        {
            Query =
                @"query($symbol:String!,$skipCount:Int!,$maxResultCount:Int!,$address:String,
                    $types:[SymbolType!],$amountGreaterThanZero:Boolean){
                    accountToken(input: {symbol:$symbol,skipCount:$skipCount,types:$types,
                    maxResultCount:$maxResultCount,address:$address,amountGreaterThanZero:$amountGreaterThanZero}){
                    totalCount,
                    items{
                        id,
                        address,
                        token {
                            symbol,
                            type,
                            decimals
                        },
                        metadata{chainId,block{blockHash,blockTime,blockHeight}},
                        amount,
                        formatAmount                      
                    }
                }
            }",
            Variables = new
            {
                symbol = symbol,
                skipCount = skipCount, 
                maxResultCount = maxResultCount, 
                address = address,
                types = SymbolType.Token,
                amountGreaterThanZero = true
            }
        });
        return indexerResult == null ? new IndexerTokenHolderInfoListDto() : indexerResult.AccountToken;
    }

    public async Task<TokenDetailDto> GetTokenDetailAsync(string symbol)
    {
        var detailDto = await _httpProvider.InvokeAsync<TokenDetailResultDto>(
            _tokenAccessOptions.ScanBaseUrl, ScanTokenDetailUri,
            param: new Dictionary<string, string> { ["symbol"] = symbol });
        return detailDto.Code == CrossChainServerConsts.SuccessHttpCode ? detailDto.Data : null;
    }
}

public class IndexerTokenHolderInfosDto
{
    public IndexerTokenHolderInfoListDto AccountToken { get; set; }
}

public class IndexerTokenBaseDto
{
    public string Symbol { get; set; }
    public SymbolType Type { get; set; }
    public int Decimals { get; set; }
}

public enum SymbolType
{
    Token,
    Nft,
    Nft_Collection
}

public class IndexerTokenHolderInfoDto
{
    public string Id { get; set; }
    public string Address { get; set; }
    public IndexerTokenBaseDto Token { get; set; }
    public long Amount { get; set; }
    public decimal FormatAmount { get; set; }
    public MetadataDto Metadata { get; set; }
}

public class IndexerTokenHolderInfoListDto
{
    public long TotalCount { get; set; }
    public List<IndexerTokenHolderInfoDto> Items { get; set; } = new();
}

public class MetadataDto
{
    public string ChainId { get; set; }

    public BlockMetadataDto Block { get; set; }
}

public class BlockMetadataDto
{
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    public DateTime BlockTime { get; set; }
}