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
    Task<List<AvailableTokenDto>> GetOwnTokensAsync(string address);
}

public class SymbolMarketProvider : ISymbolMarketProvider
{
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly IHttpProvider _httpProvider;
    private ApiInfo _myTokenUri => new(HttpMethod.Get, _tokenAccessOptions.SymbolMarketMyTokenUri);

    
    public Task IssueTokenAsync(IssueTokenInput input)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<string>> GetIssueChainListAsync(string symbol)
    {
        throw new System.NotImplementedException();
    }

    public async Task<List<AvailableTokenDto>> GetOwnTokensAsync(string address)
    {
        var addressList = new List<string>();
        foreach (var chainId in _tokenAccessOptions.ChainIdList)
        {
            addressList.Add("ELF_" + address + "_" + chainId);
        }
        var pathParams = new Dictionary<string, string>();
        pathParams["AddressList"] = JsonConvert.SerializeObject(addressList);
        var resultDto = await _httpProvider.InvokeAsync<ApiCommonResult<PagedResultDto<AvailableTokenDto>>>(_tokenAccessOptions.SymbolMarketBaseUrl, _myTokenUri, pathParams);
        if (resultDto.Code != "20000")
        {
            Log.Error("Get token tvl fail: code {code}, message: {message}", resultDto.Code, resultDto.Message);
            return new List<AvailableTokenDto>();
        }
        return resultDto.Data.Items.ToList();
    }
}