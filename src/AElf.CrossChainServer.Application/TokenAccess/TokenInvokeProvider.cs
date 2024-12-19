using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.TokenAccess;

public interface ITokenInvokeProvider
{
    Task<TokenOwnerListDto> GetUserTokenOwnerList(string address);
    Task<List<TokenOwnerDto>> GetAsync(string address);
    Task<bool> GetThirdTokenList(string address, string symbol);
}

public class TokenInvokeProvider : ITokenInvokeProvider, ITransientDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly ILogger<TokenInvokeProvider> _logger;
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly IUserTokenOwnerRepository _userTokenOwnerRepository;
    private readonly IUserTokenIssueRepository _userTokenIssueRepository;

    private const int PageSize = 50;
    private ApiInfo _scanTokenDetailUri => new(HttpMethod.Get, _tokenAccessOptions.ScanTokenDetailUri);
    private ApiInfo _tokenLiquidityUri => new(HttpMethod.Get, _tokenAccessOptions.AwakenGetTokenLiquidityUri);

    private ApiInfo _userThirdTokenListUri =>
        new(HttpMethod.Get, _tokenAccessOptions.SymbolMarketUserThirdTokenListUri);


    public TokenInvokeProvider(IHttpProvider httpProvider, IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions,
        ILogger<TokenInvokeProvider> logger, IUserTokenOwnerRepository userTokenOwnerRepository,
        IUserTokenIssueRepository userTokenIssueRepository)
    {
        _logger = logger;
        _httpProvider = httpProvider;
        _tokenAccessOptions = tokenAccessOptions.Value;
        _userTokenOwnerRepository = userTokenOwnerRepository;
        _userTokenIssueRepository = userTokenIssueRepository;
    }

    public async Task<TokenOwnerListDto> GetUserTokenOwnerList(string address)
    {
        var skipCount = 0;
        var tokenOwnerList = new TokenOwnerListDto();

        var userTokenListUri =
            $"{_tokenAccessOptions.SymbolMarketUserTokenListUri}?addressList={string.Join(CommonConstant.Underline, TokenSymbol.ELF, address, ChainId.AELF)}" +
            $"&addressList={string.Join(CommonConstant.Underline, TokenSymbol.ELF, address, ChainId.tDVV)}" +
            $"&addressList={string.Join(CommonConstant.Underline, TokenSymbol.ELF, address, ChainId.tDVW)}";
        var uri = new ApiInfo(HttpMethod.Get, userTokenListUri);
        while (true)
        {
            var resultDto = new UserTokenListResultDto();
            try
            {
                var tokenParams = new Dictionary<string, string>();
                tokenParams["skipCount"] = skipCount.ToString();
                tokenParams["maxResultCount"] = PageSize.ToString();
                resultDto = await _httpProvider.InvokeAsync<UserTokenListResultDto>(
                    _tokenAccessOptions.SymbolMarketBaseUrl, uri, param: tokenParams);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "get user tokens error.");
            }

            if (resultDto == null || resultDto.Code != "20000" || resultDto.Data == null ||
                resultDto.Data.Items.IsNullOrEmpty())
            {
                break;
            }

            skipCount += resultDto.Data.Items.Count;
            foreach (var item in resultDto.Data.Items)
            {
                var detailDto = await _httpProvider.InvokeAsync<TokenDetailResultDto>(
                    _tokenAccessOptions.ScanBaseUrl, _scanTokenDetailUri,
                    param: new Dictionary<string, string> { ["symbol"] = item.Symbol });
                tokenOwnerList.TokenOwnerList.Add(new TokenOwnerDto
                {
                    TokenName = item.TokenName,
                    Symbol = item.Symbol,
                    Decimals = item.Decimals,
                    Icon = item.TokenImage,
                    Owner = item.Owner,
                    ChainIds = detailDto?.Data?.ChainIds ?? new List<string> { item.OriginIssueChain },
                    TotalSupply = item.TotalSupply,
                    LiquidityInUsd = await GetLiquidityInUsd(item.Symbol),
                    Holders = detailDto?.Data?.Holders ?? 0,
                    ContractAddress = detailDto?.Data?.TokenContractAddress,
                    Status = TokenApplyOrderStatus.Issued.ToString()
                });
            }

            if (resultDto.Data.Items.Count < PageSize) break;
        }

        var userTokenOwner = new UserTokenOwnerDto
        {
            TokenOwnerList = tokenOwnerList.TokenOwnerList,
            Address = address
        };
        await _userTokenOwnerRepository.InsertAsync(userTokenOwner);

        return tokenOwnerList;
    }

    public async Task<List<TokenOwnerDto>> GetAsync(string address)
    {
        var result = await _userTokenOwnerRepository.FindAsync(o => o.Address == address);
        return result?.TokenOwnerList;
    }

    public async Task<bool> GetThirdTokenList(string address, string symbol)
    {
        var tokenParams = new Dictionary<string, string>();
        tokenParams["address"] = address;
        tokenParams["aelfToken"] = symbol;
        var resultDto = await _httpProvider.InvokeAsync<ThirdTokenResultDto>(
            _tokenAccessOptions.SymbolMarketBaseUrl, _userThirdTokenListUri, param: tokenParams);
        if (resultDto.Code == "20000" && resultDto.Data != null && resultDto.Data.TotalCount > 0)
        {
            foreach (var item in resultDto.Data.Items)
            {
                // var userTokenIssueGrain = GrainFactory.GetGrain<IUserTokenIssueGrain>(
                //     GuidHelper.UniqGuid(symbol, address, item.ThirdChain));
                // var res = await userTokenIssueGrain.Get();
                var res = await _userTokenIssueRepository.FindAsync(o =>
                    o.Address == address && o.OtherChainId == item.ThirdChain);
                res ??= new UserTokenIssueDto
                {
                    Address = address,
                    Symbol = item.ThirdSymbol,
                    ChainId = item.AelfChain,
                    TokenName = item.ThirdTokenName,
                    TokenImage = item.ThirdTokenImage,
                    OtherChainId = item.ThirdChain,
                    ContractAddress = item.ThirdContractAddress,
                    TotalSupply = item.ThirdTotalSupply
                };
                res.Status = TokenApplyOrderStatus.Issued.ToString();

                await _userTokenIssueRepository.InsertAsync(res);
                // await userTokenIssueGrain.AddOrUpdate(res);
            }
        }

        return false;
    }

    private async Task<string> GetLiquidityInUsd(string symbol)
    {
        var tokenParams = new Dictionary<string, string>();
        tokenParams["symbol"] = symbol;
        var resultDto =
            await _httpProvider.InvokeAsync<CommonResponseDto<string>>(_tokenAccessOptions.AwakenBaseUrl,
                _tokenLiquidityUri, param: tokenParams);
        return resultDto.Code == "20000" ? resultDto.Value : "0";
    }
}