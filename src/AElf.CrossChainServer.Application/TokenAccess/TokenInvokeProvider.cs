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
    Task<bool> GetThirdTokenListAndUpdateAsync(string address, string symbol);
    Task<UserTokenBindingDto> PrepareBindingAsync(ThirdUserTokenIssueInfoDto dto);
    Task<bool> BindingAsync(UserTokenBindingDto dto);
}

public class TokenInvokeProvider : ITokenInvokeProvider, ITransientDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly IThirdUserTokenIssueRepository _thirdUserTokenIssueRepository;
    private readonly IObjectMapper _objectMapper;

    private ApiInfo UserThirdTokenListUri =>
        new(HttpMethod.Get, _tokenAccessOptions.SymbolMarketUserThirdTokenListUri);

    public TokenInvokeProvider(IHttpProvider httpProvider, IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions,
        IThirdUserTokenIssueRepository thirdUserTokenIssueRepository, IObjectMapper objectMapper)
    {
        _httpProvider = httpProvider;
        _tokenAccessOptions = tokenAccessOptions.Value;
        _thirdUserTokenIssueRepository = thirdUserTokenIssueRepository;
        _objectMapper = objectMapper;
    }

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

        foreach (var item in resultDto.Data.Items)
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
            await _thirdUserTokenIssueRepository.UpdateAsync(existingToken, autoSave: true);
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

        // var aelfChainId = ConvertToTargetChainId(dto.ChainId);
        var thirdChainId = ConvertToTargetChainId(dto.OtherChainId);
        if (string.IsNullOrEmpty(thirdChainId))
        {
            Log.Warning("Failed to convert to target OtherChainId:{0}", dto.OtherChainId);
            return new UserTokenBindingDto();
        }

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
                ThirdChain = thirdChainId,
                Owner = dto.WalletAddress,
                ContractAddress = dto.ContractAddress
            },
            Signature = BuildRequestHash(string.Concat(dto.Address, dto.Symbol, dto.ChainId, dto.TokenName, dto.Symbol,
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
                Signature = BuildRequestHash(string.Concat(dto.BindingId, dto.ThirdTokenId, dto.TokenContractAddress,
                    dto.MintToAddress)),
                MintToAddress = dto.MintToAddress,
                TokenContractAddress = dto.TokenContractAddress
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