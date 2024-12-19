using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;
using Newtonsoft.Json;
using Serilog;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenAccess;

public interface ISymbolMarketProvider
{
    Task IssueTokenAsync(IssueTokenInput input);
    Task<List<string>> GetIssueChainListAsync(string symbol);
    Task<List<UserTokenItemDto>> GetOwnTokensAsync(string address);
}

public class SymbolMarketProvider : ISymbolMarketProvider
{
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly IHttpProvider _httpProvider;

    public SymbolMarketProvider(TokenAccessOptions tokenAccessOptions, IHttpProvider httpProvider)
    {
        _tokenAccessOptions = tokenAccessOptions;
        _httpProvider = httpProvider;
    }

    private ApiInfo MyTokenUri => new(HttpMethod.Get, _tokenAccessOptions.SymbolMarketUserTokenListUri);


    public Task IssueTokenAsync(IssueTokenInput input)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<string>> GetIssueChainListAsync(string symbol)
    {
        throw new System.NotImplementedException();
    }

    public async Task<List<UserTokenItemDto>> GetOwnTokensAsync(string address)
    {
        // var addressList = _tokenAccessOptions.ChainIdList.Select(chainId =>
        //     CrossChainServerConsts.NativeTokenSymbol + CrossChainServerConsts.Underline + address +
        //     CrossChainServerConsts.Underline + chainId).ToList();
        //
        // var pathParams = new Dictionary<string, string>();
        // pathParams["AddressList"] = JsonConvert.SerializeObject(addressList);
        // var resultDto =
        //     await _httpProvider.InvokeAsync<UserTokenListResultDto>(
        //         _tokenAccessOptions.SymbolMarketBaseUrl, MyTokenUri, pathParams);
        // if (resultDto.Code != "20000")
        // {
        //     Log.Error("Get token tvl fail: code {code}, message: {message}", resultDto.Code, resultDto.Message);
        //     return new List<UserTokenListResultDto>();
        // }
        //
        // return resultDto.Data.Items.ToList();
        return null;
    }
}