using System;
using System.Collections.Generic;
using System.Linq;
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
    Task<TokenOwnerListDto> GetUserTokenOwnerListAndUpdateAsync(string address);
    Task<List<UserTokenOwner>> GetAsync(string address);
    Task<bool> GetThirdTokenListAndUpdateAsync(string address, string symbol);
    Task<UserTokenBindingDto> PrepareBindingAsync(UserTokenIssueDto dto);
    Task<bool> BindingAsync(UserTokenBindingDto dto);
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

    public async Task<TokenOwnerListDto> GetUserTokenOwnerListAndUpdateAsync(string address)
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

        var userOwnerTokens = await _userTokenOwnerRepository.GetListAsync(o => o.Address == address);
        var createPendingUserOwnerTokens = new List<UserTokenOwner>();
        var updatePendingUserOwnerTokens = new List<UserTokenOwner>();
        foreach (var token in tokenOwnerList.TokenOwnerList)
        {
            var symbolExistOwnerTokens = userOwnerTokens.FindAll(u => u.Symbol == token.Symbol);
            if (symbolExistOwnerTokens.Count == 0)
            {
                var owners = token.ChainIds.Select(t => new UserTokenOwner
                {
                    TokenName = token.TokenName,
                    Symbol = token.Symbol,
                    Decimals = token.Decimals,
                    Icon = token.Icon,
                    Owner = token.Owner,
                    ChainId = t,
                    TotalSupply = token.TotalSupply,
                    LiquidityInUsd = token.LiquidityInUsd,
                    Holders = token.Holders,
                    ContractAddress = token.ContractAddress,
                    Status = TokenApplyOrderStatus.Issued.ToString(),
                    Address = address
                });
                createPendingUserOwnerTokens.AddRange(owners);
            }
            else
            {
                var tokenOwnerChainIds = new HashSet<string>(symbolExistOwnerTokens.Select(owner => owner.ChainId));
                var missingChainIdToken = token.ChainIds.Where(chainId => !tokenOwnerChainIds.Contains(chainId)).Select(
                    c => new UserTokenOwner
                    {
                        TokenName = token.TokenName,
                        Symbol = token.Symbol,
                        Decimals = token.Decimals,
                        Icon = token.Icon,
                        Owner = token.Owner,
                        ChainId = c,
                        TotalSupply = token.TotalSupply,
                        LiquidityInUsd = token.LiquidityInUsd,
                        Holders = token.Holders,
                        ContractAddress = token.ContractAddress,
                        Status = TokenApplyOrderStatus.Issued.ToString(),
                        Address = address
                    });

                var chainIdsSet = new HashSet<string>(token.ChainIds);
                var matchingTokenOwners =
                    symbolExistOwnerTokens.Where(owner => chainIdsSet.Contains(owner.ChainId)).ToList();
                matchingTokenOwners.ForEach(t => t.Status = TokenApplyOrderStatus.Issued.ToString());
                updatePendingUserOwnerTokens.AddRange(matchingTokenOwners);
                createPendingUserOwnerTokens.AddRange(missingChainIdToken);
            }
        }

        await _userTokenOwnerRepository.InsertManyAsync(createPendingUserOwnerTokens);
        await _userTokenOwnerRepository.UpdateManyAsync(updatePendingUserOwnerTokens);
        return tokenOwnerList;
    }

    public async Task<List<UserTokenOwner>> GetAsync(string address)
    {
        var result = await _userTokenOwnerRepository.GetListAsync(o => o.Address == address);
        return result;
    }

    public async Task<bool> GetThirdTokenListAndUpdateAsync(string address, string symbol)
    {
        try
        {
            var tokenParams = new Dictionary<string, string>();
            tokenParams["address"] = address;
            tokenParams["aelfToken"] = symbol;
            var resultDto = await _httpProvider.InvokeAsync<ThirdTokenResultDto>(
                _tokenAccessOptions.SymbolMarketBaseUrl, _userThirdTokenListUri, param: tokenParams);
            if (resultDto.Code != "20000" || resultDto.Data == null)
            {
                _logger.LogWarning($"get {address} {symbol} failed, message: {resultDto.Message}");
                return false;
            }

            foreach (var item in resultDto.Data)
            {
                var aelfChainId = FindMatchChainId(item.AelfChain);
                var thirdChainId = FindMatchChainId(item.ThirdChain);
                if (string.IsNullOrWhiteSpace(aelfChainId) || string.IsNullOrWhiteSpace(thirdChainId))
                {
                    _logger.LogWarning(
                        $"skip not supported chainId, aelf chain: {aelfChainId} third chain: {thirdChainId}");
                    break;
                }

                var res = await _userTokenIssueRepository.FindAsync(o =>
                    o.Address == address && o.Symbol == item.ThirdSymbol && o.OtherChainId == thirdChainId);
                if (res != null)
                {
                    _logger.LogDebug(
                        $"Update address: {address}, Symbol: {item.ThirdSymbol}, OtherChainId: {thirdChainId} TokenIssue");
                    res.Status = TokenApplyOrderStatus.Issued.ToString();
                    await _userTokenIssueRepository.UpdateAsync(res);
                }
                else
                {
                    _logger.LogDebug(
                        $"Create address: {address}, Symbol: {item.ThirdSymbol}, OtherChainId: {thirdChainId} TokenIssue");
                    await _userTokenIssueRepository.InsertAsync(new UserTokenIssueDto
                    {
                        Address = address,
                        Symbol = item.ThirdSymbol,
                        ChainId = aelfChainId,
                        TokenName = item.ThirdTokenName,
                        TokenImage = item.ThirdTokenImage,
                        OtherChainId = thirdChainId,
                        ContractAddress = item.ThirdContractAddress,
                        TotalSupply = item.ThirdTotalSupply,
                        Status = TokenApplyOrderStatus.Issued.ToString()
                    });
                }
            }

            return false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get SymbolMarket failed");
            return false;
        }
    }

    public async Task<UserTokenBindingDto> PrepareBindingAsync(UserTokenIssueDto dto)
    {
        var res = await _userTokenIssueRepository.FindAsync(o =>
            o.Address == dto.Address && o.Symbol == dto.Symbol && o.OtherChainId == dto.OtherChainId);
        if (res != null && !res.BindingId.IsNullOrEmpty() && !res.ThirdTokenId.IsNullOrEmpty())
        {
            _logger.LogDebug("Get existing TokenIssue, no need request symbol market again.");
            return new UserTokenBindingDto { BindingId = res.BindingId, ThirdTokenId = res.ThirdTokenId };
        }

        var url =
            $"{_tokenAccessOptions.SymbolMarketBaseUrl}{_tokenAccessOptions.SymbolMarketPrepareBindingUri}";
        var prepareBindingInput = new PrepareBindingInput
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
        };
        var resultDto = await _httpProvider.InvokeAsync<PrepareBindingResultDto>(HttpMethod.Post, url,
            body: JsonConvert.SerializeObject(prepareBindingInput, HttpProvider.DefaultJsonSettings));
        if (resultDto.Code != "20000") return new UserTokenBindingDto();


        var bindingId = resultDto.Data?.BindingId;
        var thirdTokenId = resultDto.Data?.ThirdTokenId;
        var status = TokenApplyOrderStatus.Issuing.ToString();
        if (res != null)
        {
            _logger.LogDebug(
                $"get exist TokenIssue, will update BindingId, ThirdTokenId and status {res.Status} to issuing");
            res.BindingId = bindingId;
            res.ThirdTokenId = thirdTokenId;
            res.Status = status;
            await _userTokenIssueRepository.UpdateAsync(res);
        }
        else
        {
            _logger.LogDebug($"create TokenIssue, will update {dto.Id} status to issuing");
            dto.BindingId = bindingId;
            dto.ThirdTokenId = thirdTokenId;
            dto.Status = status;
            await _userTokenIssueRepository.InsertAsync(dto);
        }

        _logger.LogDebug("Insert token invoke");
        return new UserTokenBindingDto { BindingId = bindingId, ThirdTokenId = thirdTokenId };
    }

    public async Task<bool> BindingAsync(UserTokenBindingDto dto)
    {
        var userTokenIssue = await _userTokenIssueRepository.FindAsync(o =>
            o.BindingId == dto.BindingId && o.ThirdTokenId == dto.ThirdTokenId);
        if (userTokenIssue == null)
        {
            _logger.LogDebug($"Token {dto.BindingId} {dto.ThirdTokenId} not exist.");
            return false;
        }

        if (userTokenIssue.Status == TokenApplyOrderStatus.Issued.ToString())
        {
            _logger.LogDebug($"Skip, Token {dto.BindingId} {dto.ThirdTokenId} had issued");
            return true;
        }

        var url = $"{_tokenAccessOptions.SymbolMarketBaseUrl}{_tokenAccessOptions.SymbolMarketBindingUri}";
        var resultDto = await _httpProvider.InvokeAsync<CommonResponseDto<string>>(HttpMethod.Post, url,
            body: JsonConvert.SerializeObject(new BindingInput
            {
                BindingId = dto.BindingId,
                ThirdTokenId = dto.ThirdTokenId,
                Signature = BuildRequestHash(string.Concat(dto.BindingId, dto.ThirdTokenId))
            }, HttpProvider.DefaultJsonSettings));
        if (resultDto.Code != "20000")
        {
            _logger.LogWarning($"request symbol market fail, {resultDto.Message}");
            return false;
        }

        userTokenIssue.BindingId = dto.BindingId;
        userTokenIssue.ThirdTokenId = dto.ThirdTokenId;
        userTokenIssue.Status = TokenApplyOrderStatus.Issued.ToString();
        await _userTokenIssueRepository.UpdateAsync(userTokenIssue);
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

    private string FindMatchChainId(string sourceChainId)
        => _tokenAccessOptions.ChainIdMap.TryGetValue(sourceChainId, out string value) ? value : string.Empty;
}