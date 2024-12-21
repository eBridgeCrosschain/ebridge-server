using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.TokenAccess;

public interface ITokenInvokeProvider
{
    Task<TokenOwnerListDto> GetUserTokenOwnerList(string address);
    Task<List<TokenOwnerDto>> GetAsync(string address);
    Task<bool> GetThirdTokenList(string address, string symbol);
    Task<UserTokenBindingDto> PrepareBinding(UserTokenIssueDto dto);
    Task<bool> Binding(UserTokenBindingDto dto);
}

public class TokenInvokeProvider : ITokenInvokeProvider, ITransientDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly ILogger<TokenInvokeProvider> _logger;
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly ITokenInvokeRepository _invokeRepository;
    private readonly IUserTokenOwnerRepository _userTokenOwnerRepository;
    private readonly IUserTokenIssueRepository _userTokenIssueRepository;

    private const int PageSize = 50;
    private ApiInfo _scanTokenDetailUri => new(HttpMethod.Get, _tokenAccessOptions.ScanTokenDetailUri);
    private ApiInfo _tokenLiquidityUri => new(HttpMethod.Get, _tokenAccessOptions.AwakenGetTokenLiquidityUri);

    private ApiInfo _userThirdTokenListUri =>
        new(HttpMethod.Get, _tokenAccessOptions.SymbolMarketUserThirdTokenListUri);


    public TokenInvokeProvider(IHttpProvider httpProvider, IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions,
        ILogger<TokenInvokeProvider> logger, IUserTokenOwnerRepository userTokenOwnerRepository,
        IUserTokenIssueRepository userTokenIssueRepository, ITokenInvokeRepository invokeRepository)
    {
        _logger = logger;
        _httpProvider = httpProvider;
        _tokenAccessOptions = tokenAccessOptions.Value;
        _userTokenOwnerRepository = userTokenOwnerRepository;
        _userTokenIssueRepository = userTokenIssueRepository;
        _invokeRepository = invokeRepository;
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

        var entity = await _userTokenOwnerRepository.FindAsync(o => o.Address == address);
        if (entity != null)
        {
            entity.TokenOwnerList = tokenOwnerList.TokenOwnerList;
            await _userTokenOwnerRepository.UpdateAsync(entity);
        }
        else
        {
            var userTokenOwner = new UserTokenOwnerDto
            {
                TokenOwnerList = tokenOwnerList.TokenOwnerList,
                Address = address
            };
            await _userTokenOwnerRepository.InsertAsync(userTokenOwner, autoSave: true);
        }

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
        if (resultDto.Code != "20000" || resultDto.Data == null || resultDto.Data.TotalCount <= 0) return false;

        foreach (var item in resultDto.Data.Items)
        {
            var res = await _userTokenIssueRepository.FindAsync(o =>
                o.Address == address && o.OtherChainId == item.ThirdChain);
            if (res != null)
            {
                res.Status = TokenApplyOrderStatus.Issued.ToString();
                await _userTokenIssueRepository.UpdateAsync(res);
            }
            else
            {
                await _userTokenIssueRepository.InsertAsync(new UserTokenIssueDto
                {
                    Address = address,
                    Symbol = item.ThirdSymbol,
                    ChainId = item.AelfChain,
                    TokenName = item.ThirdTokenName,
                    TokenImage = item.ThirdTokenImage,
                    OtherChainId = item.ThirdChain,
                    ContractAddress = item.ThirdContractAddress,
                    TotalSupply = item.ThirdTotalSupply,
                    Status = TokenApplyOrderStatus.Issued.ToString()
                });
            }
        }

        return false;
    }

    public async Task<UserTokenBindingDto> PrepareBinding(UserTokenIssueDto dto)
    {
        var res = await _userTokenIssueRepository.FindAsync(o =>
            o.Address == dto.Address && o.Symbol == dto.Symbol && o.OtherChainId == dto.OtherChainId);
        if (res != null && !res.BindingId.IsNullOrEmpty() && !res.ThirdTokenId.IsNullOrEmpty())
            return new UserTokenBindingDto { BindingId = res.BindingId, ThirdTokenId = res.ThirdTokenId };

        var url =
            $"{_tokenAccessOptions.SymbolMarketBaseUrl}{_tokenAccessOptions.SymbolMarketPrepareBindingUri}";
        var resultDto = await _httpProvider.InvokeAsync<PrepareBindingResultDto>(HttpMethod.Post, url,
            body: JsonConvert.SerializeObject(new PrepareBindingInput
            {
                Address = dto.Address,
                AelfToken = dto.Symbol,
                AelfChain = dto.ChainId,
                ThirdTokens = new ThirdTokenDto
                {
                    TokenName = dto.TokenName,
                    Symbol = dto.Symbol,
                    TokenImage = dto.TokenImage,
                    TotalSupply = dto.TotalSupply,
                    ThirdChain = dto.OtherChainId,
                    Owner = dto.WalletAddress,
                    ContractAddress = dto.ContractAddress
                },
                Signature = BuildRequestHash(string.Concat(dto.Address, dto.Symbol, dto.ChainId, dto.TokenName,
                    dto.Symbol, dto.TokenImage, dto.TotalSupply, dto.WalletAddress, dto.OtherChainId,
                    dto.ContractAddress))
            }, HttpProvider.DefaultJsonSettings));
        if (resultDto.Code != "20000") return new UserTokenBindingDto();

        dto.BindingId = resultDto.Data?.BindingId;
        dto.ThirdTokenId = resultDto.Data?.ThirdTokenId;
        dto.Status = TokenApplyOrderStatus.Issuing.ToString();
        await _userTokenIssueRepository.InsertAsync(dto);
        await _invokeRepository.InsertAsync(new()
        {
            BindingId = dto.BindingId,
            UserTokenIssueId = dto.Id,
            ThirdTokenId = dto.ThirdTokenId
        });
        return new UserTokenBindingDto { BindingId = dto.BindingId, ThirdTokenId = dto.ThirdTokenId };
    }

    public async Task<bool> Binding(UserTokenBindingDto dto)
    {
        var tokenInvoke = await _invokeRepository.FindAsync(o =>
            o.BindingId == dto.BindingId && o.ThirdTokenId == dto.ThirdTokenId);
        if (tokenInvoke != null && tokenInvoke.UserTokenIssueId != Guid.Empty)
        {
            var res = await _userTokenIssueRepository.FindAsync(o => o.Id == tokenInvoke.UserTokenIssueId);
            if (res != null && res.Status == TokenApplyOrderStatus.Issued.ToString()) return true;
        }

        var url = $"{_tokenAccessOptions.SymbolMarketBaseUrl}{_tokenAccessOptions.SymbolMarketBindingUri}";
        var resultDto = await _httpProvider.InvokeAsync<CommonResponseDto<string>>(HttpMethod.Post, url,
            body: JsonConvert.SerializeObject(new BindingInput
            {
                BindingId = dto.BindingId,
                ThirdTokenId = dto.ThirdTokenId,
                Signature = BuildRequestHash(string.Concat(dto.BindingId, dto.ThirdTokenId))
            }, HttpProvider.DefaultJsonSettings));
        if (resultDto.Code != "20000" || tokenInvoke == null || tokenInvoke.UserTokenIssueId == Guid.Empty)
            return false;

        var tokenIssueFindByIssueId =
            await _userTokenIssueRepository.FindAsync(o => o.Id == tokenInvoke.UserTokenIssueId);
        tokenIssueFindByIssueId.BindingId = dto.BindingId;
        tokenIssueFindByIssueId.ThirdTokenId = dto.ThirdTokenId;
        tokenIssueFindByIssueId.Status = TokenApplyOrderStatus.Issued.ToString();
        await _userTokenIssueRepository.UpdateAsync(tokenIssueFindByIssueId);
        return true;
    }

    private async Task<string> GetLiquidityInUsd(string symbol)
    {
        var tokenParams = new Dictionary<string, string>();
        tokenParams["symbol"] = symbol;
        var resultDto = await _httpProvider.InvokeAsync<CommonResponseDto<string>>(_tokenAccessOptions.AwakenBaseUrl,
            _tokenLiquidityUri, param: tokenParams);
        return resultDto.Code == "20000" ? resultDto.Value : "0";
    }

    private string BuildRequestHash(string request)
    {
        var hashVerifyKey = _tokenAccessOptions.HashVerifyKey;
        var requestHash = HashHelper.ComputeFrom(string.Concat(request, hashVerifyKey));
        return requestHash.ToHex();
    }
}