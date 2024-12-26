using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;
using AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.TokenAccess;

public interface ITokenInvokeProvider
{
    Task<List<UserTokenOwnerInfoDto>> GetUserTokenOwnerListAndUpdateAsync(string address);
    Task<List<UserTokenOwnerInfoDto>> GetAsync(string address);
    Task<bool> GetThirdTokenListAndUpdateAsync(string address, string symbol);
    Task<UserTokenBindingDto> PrepareBindingAsync(ThirdUserTokenIssueInfoDto dto);
    Task<bool> BindingAsync(UserTokenBindingDto dto);
}

public class TokenInvokeProvider : ITokenInvokeProvider, ITransientDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly IUserTokenOwnerProvider _userTokenOwnerProvider;
    private readonly IThirdUserTokenIssueRepository _thirdUserTokenIssueRepository;
    private readonly IObjectMapper _objectMapper;

    private const int PageSize = 50;
    private ApiInfo ScanTokenDetailUri => new(HttpMethod.Get, _tokenAccessOptions.ScanTokenDetailUri);
    private ApiInfo TokenLiquidityUri => new(HttpMethod.Get, _tokenAccessOptions.AwakenGetTokenLiquidityUri);

    private ApiInfo UserThirdTokenListUri =>
        new(HttpMethod.Get, _tokenAccessOptions.SymbolMarketUserThirdTokenListUri);


    public TokenInvokeProvider(IHttpProvider httpProvider, IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions,
        IThirdUserTokenIssueRepository thirdUserTokenIssueRepository, IObjectMapper objectMapper,
        IUserTokenOwnerProvider userTokenOwnerProvider)
    {
        _httpProvider = httpProvider;
        _tokenAccessOptions = tokenAccessOptions.Value;
        _thirdUserTokenIssueRepository = thirdUserTokenIssueRepository;
        _objectMapper = objectMapper;
        _userTokenOwnerProvider = userTokenOwnerProvider;
    }

    public async Task<List<UserTokenOwnerInfoDto>> GetUserTokenOwnerListAndUpdateAsync(string address)
    {
        var skipCount = 0;
        var tokenOwnerList = new List<UserTokenOwnerInfoDto>();

        while (true)
        {
            UserTokenListResultDto resultDto;
            try
            {
                resultDto = await FetchUserTokenListAsync(address, skipCount);
                if (!IsValidResult(resultDto)) break;

                await ProcessItemsAsync(resultDto.Data.Items, address, tokenOwnerList);

                if (resultDto.Data.Items.Count < PageSize) break;

                skipCount += resultDto.Data.Items.Count;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to fetch user tokens for address {Address}.", address);
                break;
            }
        }

        await _userTokenOwnerProvider.AddUserTokenOwnerListAsync(address, tokenOwnerList);

        return tokenOwnerList;
    }

    public async Task<List<UserTokenOwnerInfoDto>> GetAsync(string address)
        => await _userTokenOwnerProvider.GetUserTokenOwnerListAsync(address);

    [ExceptionHandler(typeof(Exception),
        Message = "[GetThirdTokenListAndUpdateAsync] Get third token from symbolMarket failed.",
        ReturnDefault = ReturnDefault.Default, LogTargets = new[] { "address", "symbol" })]
    public async Task<bool> GetThirdTokenListAndUpdateAsync(string address, string symbol)
    {
        var resultDto = await FetchThirdTokenListAsync(address, symbol);
        if (resultDto.Code != "20000" || resultDto.Data == null)
        {
            Log.Warning("Get {address} {symbol} failed, message: {message}", address, symbol, resultDto.Message);
            return false;
        }

        foreach (var item in resultDto.Data)
        {
            var aelfChainId = FindMatchChainId(item.AelfChain);
            var thirdChainId = FindMatchChainId(item.ThirdChain);

            if (string.IsNullOrWhiteSpace(aelfChainId) || string.IsNullOrWhiteSpace(thirdChainId))
            {
                Log.Warning("skip not supported chainId, aelf chain: {aelfChainId} third chain: {thirdChainId}",
                    item.AelfChain, item.ThirdChain);
                continue;
            }

            await UpsertThirdTokenAsync(address, item, aelfChainId, thirdChainId);
        }

        return false;
    }

    private async Task<UserTokenListResultDto> FetchUserTokenListAsync(string address, int skipCount)
    {
        var chainIds = new List<string> { ChainId.AELF, ChainId.tDVV, ChainId.tDVW };
        var addressListParams = string.Join("&", chainIds.Select(chainId =>
            $"addressList={string.Join(CommonConstant.Underline, TokenSymbol.ELF, address, chainId)}"));
        var userTokenListUri = $"{_tokenAccessOptions.SymbolMarketUserTokenListUri}?{addressListParams}";
        var uri = new ApiInfo(HttpMethod.Get, userTokenListUri);

        var tokenParams = new Dictionary<string, string>
        {
            ["skipCount"] = skipCount.ToString(),
            ["maxResultCount"] = PageSize.ToString()
        };

        return await _httpProvider.InvokeAsync<UserTokenListResultDto>(
            _tokenAccessOptions.SymbolMarketBaseUrl, uri, param: tokenParams);
    }

    private async Task<ThirdTokenResultDto> FetchThirdTokenListAsync(string address, string symbol)
    {
        var tokenParams = new Dictionary<string, string>
        {
            ["address"] = address,
            ["aelfToken"] = symbol
        };

        return await _httpProvider.InvokeAsync<ThirdTokenResultDto>(
            _tokenAccessOptions.SymbolMarketBaseUrl, UserThirdTokenListUri, param: tokenParams);
    }

    private bool IsValidResult(UserTokenListResultDto resultDto)
        => resultDto?.Code == "20000" && resultDto.Data?.Items != null && resultDto.Data.Items.Any();

    private async Task ProcessItemsAsync(List<UserTokenItemDto> items, string address,
        ICollection<UserTokenOwnerInfoDto> infos)
    {
        foreach (var item in items)
        {
            var detailDto = await _httpProvider.InvokeAsync<TokenDetailResultDto>(
                _tokenAccessOptions.ScanBaseUrl, ScanTokenDetailUri,
                param: new Dictionary<string, string> { ["symbol"] = item.Symbol });

            foreach (var userTokenOwnerInfo in _tokenAccessOptions.ChainIdList.Select(chainId =>
                         CreateUserTokenOwnerInfo(item, address, chainId, detailDto)))
            {
                userTokenOwnerInfo.LiquidityInUsd = await GetLiquidityInUsd(item.Symbol);
                infos.Add(userTokenOwnerInfo);
            }
        }
    }

    private UserTokenOwnerInfoDto CreateUserTokenOwnerInfo(UserTokenItemDto item, string address, string chainId,
        TokenDetailResultDto detailDto)
    {
        var userTokenOwnerInfo = _objectMapper.Map<UserTokenItemDto, UserTokenOwnerInfoDto>(item);
        userTokenOwnerInfo.Address = address;
        userTokenOwnerInfo.ChainId = chainId;
        userTokenOwnerInfo.ContractAddress = chainId == CrossChainServerConsts.AElfMainChainId
            ? detailDto?.Data?.TokenContractAddress
            : _tokenAccessOptions.DAppChainTokenContractAddress;
        userTokenOwnerInfo.Holders = detailDto?.Data?.Holders ?? 0;
        userTokenOwnerInfo.Status = TokenApplyOrderStatus.Issued.ToString();
        return userTokenOwnerInfo;
    }

    private async Task UpsertThirdTokenAsync(string address, ThirdTokenItemDto item, string aelfChainId,
        string thirdChainId)
    {
        var existingToken = await _thirdUserTokenIssueRepository.FindAsync(o =>
            o.Address == address && o.Symbol == item.AelfToken && o.OtherChainId == thirdChainId);

        if (existingToken != null)
        {
            Log.Debug("Update third token, user: {address}, Symbol: {symbol}, OtherChainId: {thirdChainId} TokenIssue",
                address, item.AelfToken, thirdChainId);
            existingToken.Status = TokenApplyOrderStatus.Issued.ToString();
            await _thirdUserTokenIssueRepository.UpdateAsync(existingToken);
        }
        else
        {
            Log.Debug("Add third token, user: {address}, Symbol: {symbol}, OtherChainId: {thirdChainId} TokenIssue",
                address, item.ThirdSymbol, thirdChainId);
            var info = _objectMapper.Map<ThirdTokenItemDto, ThirdUserTokenIssueInfo>(item);
            info.Address = address;
            info.Symbol = item.AelfToken;
            info.ChainId = aelfChainId;
            info.OtherChainId = thirdChainId;
            info.Status = TokenApplyOrderStatus.Issued.ToString();
            await _thirdUserTokenIssueRepository.InsertAsync(info, autoSave: true);
        }
    }

    public async Task<UserTokenBindingDto> PrepareBindingAsync(ThirdUserTokenIssueInfoDto dto)
    {
        var res = await _thirdUserTokenIssueRepository.FindAsync(o =>
            o.Address == dto.Address && o.Symbol == dto.Symbol && o.OtherChainId == dto.OtherChainId);
        if (res != null && !res.BindingId.IsNullOrEmpty() && !res.ThirdTokenId.IsNullOrEmpty())
        {
            Log.Debug("Get existing TokenIssue, no need request symbol market again.");
            return new UserTokenBindingDto { BindingId = res.BindingId, ThirdTokenId = res.ThirdTokenId };
        }

        var aelfChainId = ConvertToTargetChainId(dto.ChainId);
        var thirdChainId = ConvertToTargetChainId(dto.OtherChainId);
        if (string.IsNullOrEmpty(aelfChainId) || string.IsNullOrEmpty(thirdChainId))
        {
            Log.Warning("Failed to convert to target chainId:{0} OtherChainId:{1}", dto.ChainId, dto.OtherChainId);
            return new UserTokenBindingDto();
        }

        var prepareBindingInput = new PrepareBindingInput
        {
            Address = dto.Address,
            AelfToken = dto.Symbol,
            AelfChain = aelfChainId,
            ThirdTokens = new ThirdTokenDto
            {
                TokenName = dto.TokenName,
                Symbol = dto.Symbol,
                TokenImage = dto.TokenImage,
                TotalSupply = dto.TotalSupply,
                ThirdChain = thirdChainId,
                Owner = dto.WalletAddress,
                ContractAddress = dto.ContractAddress
            },
            Signature = BuildRequestHash(string.Concat(dto.Address, dto.Symbol, aelfChainId, dto.TokenName, dto.Symbol,
                dto.TokenImage, dto.TotalSupply, dto.WalletAddress, thirdChainId, dto.ContractAddress))
        };
        var url = $"{_tokenAccessOptions.SymbolMarketBaseUrl}{_tokenAccessOptions.SymbolMarketPrepareBindingUri}";
        var resultDto = await _httpProvider.InvokeAsync<PrepareBindingResultDto>(HttpMethod.Post, url,
            body: JsonConvert.SerializeObject(prepareBindingInput, HttpProvider.DefaultJsonSettings));
        if (resultDto.Code != "20000")
        {
            Log.Warning("Request forest prepare binding failed, {ERR}", resultDto.Message);
            return new UserTokenBindingDto();
        }

        var bindingId = resultDto.Data?.BindingId;
        var thirdTokenId = resultDto.Data?.ThirdTokenId;
        var status = TokenApplyOrderStatus.Issuing.ToString();
        if (res != null)
        {
            Log.Debug(
                "get exist TokenIssue, will update BindingId, ThirdTokenId and status {status} to issuing", res.Status);
            res.BindingId = bindingId;
            res.ThirdTokenId = thirdTokenId;
            res.Status = status;
            await _thirdUserTokenIssueRepository.UpdateAsync(res);
        }
        else
        {
            Log.Debug("create TokenIssue, will update {symbol},{chainId},{otherChainId} status to issuing",
                dto.Symbol, dto.ChainId, dto.OtherChainId);
            var info = _objectMapper.Map<ThirdUserTokenIssueInfoDto, ThirdUserTokenIssueInfo>(dto);
            info.BindingId = bindingId;
            info.ThirdTokenId = thirdTokenId;
            info.Status = status;
            await _thirdUserTokenIssueRepository.InsertAsync(info);
        }

        return new UserTokenBindingDto { BindingId = bindingId, ThirdTokenId = thirdTokenId };
    }

    public async Task<bool> BindingAsync(UserTokenBindingDto dto)
    {
        var userTokenIssue = await _thirdUserTokenIssueRepository.FindAsync(o =>
            o.BindingId == dto.BindingId && o.ThirdTokenId == dto.ThirdTokenId);
        if (userTokenIssue == null)
        {
            Log.Debug($"Token {dto.BindingId} {dto.ThirdTokenId} not exist.");
            return false;
        }

        if (userTokenIssue.Status == TokenApplyOrderStatus.Issued.ToString())
        {
            Log.Debug($"Skip, Token {dto.BindingId} {dto.ThirdTokenId} had issued");
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
            Log.Warning($"request symbol market fail, {resultDto.Message}");
            return false;
        }

        userTokenIssue.BindingId = dto.BindingId;
        userTokenIssue.ThirdTokenId = dto.ThirdTokenId;
        userTokenIssue.Status = TokenApplyOrderStatus.Issued.ToString();
        await _thirdUserTokenIssueRepository.UpdateAsync(userTokenIssue);
        return true;
    }

    private async Task<string> GetLiquidityInUsd(string symbol)
    {
        var tokenParams = new Dictionary<string, string>();
        tokenParams["symbol"] = symbol;
        var resultDto = await _httpProvider.InvokeAsync<CommonResponseDto<string>>(_tokenAccessOptions.AwakenBaseUrl,
            TokenLiquidityUri, param: tokenParams);
        return resultDto.Code == "20000" ? resultDto.Value : "0";
    }

    private string BuildRequestHash(string request)
    {
        var hashVerifyKey = _tokenAccessOptions.HashVerifyKey;
        var requestHash = HashHelper.ComputeFrom(string.Concat(request, hashVerifyKey));
        return requestHash.ToHex();
    }

    private string FindMatchChainId(string sourceChainId)
        => _tokenAccessOptions.ChainIdMap.TryGetValue(sourceChainId, out var value) ? value : string.Empty;

    private string ConvertToTargetChainId(string sourceChainId)
        => _tokenAccessOptions.ChainIdMap.FirstOrDefault(kvp => kvp.Value == sourceChainId).Key ?? string.Empty;
}