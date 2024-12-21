using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.TokenPrice;
using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace AElf.CrossChainServer.TokenAccess;

[RemoteService(IsEnabled = false)]
public class TokenAccessAppService : CrossChainServerAppService, ITokenAccessAppService
{
    // private readonly ISymbolMarketProvider _symbolMarketProvider;
    // private readonly ILiquidityDataProvider _liquidityDataProvider;
    // private readonly IScanProvider _scanProvider;
    // private readonly ITokenApplyOrderRepository _tokenApplyOrderRepository;
    // private readonly AElfClientProvider _aElfClientProvider;
    // private readonly ILarkManager _larkManager;
    // private readonly IUserAccessTokenInfoRepository _userAccessTokenInfoRepository;
    private readonly IUserAccessTokenInfoRepository _userAccessTokenInfoRepository;
    private readonly IUserTokenIssueRepository _userTokenIssueRepository;
    private readonly ICrossChainUserRepository _crossChainUserRepository;
    private readonly INESTRepository<TokenApplyOrderIndex, Guid> _tokenApplyOrderIndexRepository;
    private readonly INESTRepository<UserTokenAccessInfoIndex, Guid> _userAccessTokenInfoIndexRepository;
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly TokenWhitelistOptions _tokenWhitelistOptions;
    private readonly IPoolLiquidityInfoAppService _poolLiquidityInfoAppService;
    private readonly IUserLiquidityInfoAppService _userLiquidityInfoAppService;
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly TokenPriceIdMappingOptions _tokenPriceIdMappingOptions;
    private const int MaxMaxResultCount = 1000;
    private const int DefaultSkipCount = 0;
    private readonly IChainAppService _chainAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly ITokenInvokeProvider _tokenInvokeProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly NetworkOptions _networkInfoOptions;
    private readonly TokenOptions _tokenOptions;
    private readonly TokenInfoOptions _tokenInfoOptions;

    public TokenAccessAppService(
        // ISymbolMarketProvider symbolMarketProvider,
        // ILiquidityDataProvider liquidityDataProvider,
        // IScanProvider scanProvider, ITokenApplyOrderRepository tokenApplyOrderRepository,
        // ILarkManager larkManager, 
        // AElfClientProvider aElfClientProvider,
        IUserTokenIssueRepository userTokenIssueRepository,
        ICrossChainUserRepository crossChainUserRepository,
        // IUserAccessTokenInfoRepository userAccessTokenInfoRepository,
        INESTRepository<TokenApplyOrderIndex, Guid> tokenApplyOrderIndexRepository,
        INESTRepository<UserTokenAccessInfoIndex, Guid> userAccessTokenInfoIndexRepository,
        IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions,
        IOptionsSnapshot<TokenWhitelistOptions> tokenWhitelistOptions,
        IOptionsSnapshot<TokenPriceIdMappingOptions> tokenPriceIdMappingOptions,
        IPoolLiquidityInfoAppService poolLiquidityInfoAppService,
        IUserLiquidityInfoAppService userLiquidityInfoAppService,
        ITokenPriceProvider tokenPriceProvider,
        IChainAppService chainAppService, ITokenAppService tokenAppService,
        ITokenInvokeProvider tokenInvokeProvider,
        IObjectMapper objectMapper, IOptionsSnapshot<NetworkOptions> networkInfoOptions,
        IOptionsSnapshot<TokenOptions> tokenOptions, IOptionsSnapshot<TokenInfoOptions> tokenInfoOptions,
        IUserAccessTokenInfoRepository userAccessTokenInfoRepository)
    {
        // _symbolMarketProvider = symbolMarketProvider;
        // _liquidityDataProvider = liquidityDataProvider;
        // _scanProvider = scanProvider;
        // _tokenApplyOrderRepository = tokenApplyOrderRepository;
        _userTokenIssueRepository = userTokenIssueRepository;
        _crossChainUserRepository = crossChainUserRepository;
        // _userAccessTokenInfoRepository = userAccessTokenInfoRepository;
        _tokenApplyOrderIndexRepository = tokenApplyOrderIndexRepository;
        _userAccessTokenInfoIndexRepository = userAccessTokenInfoIndexRepository;
        // _aElfClientProvider = aElfClientProvider;
        _tokenAccessOptions = tokenAccessOptions.Value;
        // _larkManager = larkManager;
        _poolLiquidityInfoAppService = poolLiquidityInfoAppService;
        _userLiquidityInfoAppService = userLiquidityInfoAppService;
        _tokenPriceProvider = tokenPriceProvider;
        _chainAppService = chainAppService;
        _tokenAppService = tokenAppService;
        _tokenInvokeProvider = tokenInvokeProvider;
        _objectMapper = objectMapper;
        _userAccessTokenInfoRepository = userAccessTokenInfoRepository;
        _tokenInfoOptions = tokenInfoOptions.Value;
        _tokenOptions = tokenOptions.Value;
        _networkInfoOptions = networkInfoOptions.Value;
        _tokenWhitelistOptions = tokenWhitelistOptions.Value;
        _tokenPriceIdMappingOptions = tokenPriceIdMappingOptions.Value;
    }

    // public async Task<AvailableTokensDto> GetAvailableTokensAsync()
    // {
    //     // todo : get address from jwt token
    //     // string address = null;
    //     // var symbolMarketTokenList = await _symbolMarketProvider.GetOwnTokensAsync(address);
    //     // tokenList.AddRange(symbolMarketTokenList);
    //     // foreach (var token in tokenList)
    //     // {
    //     //     token.LiquidityInUsd = await _liquidityDataProvider.GetTokenTvlAsync(token.Symbol);
    //     // }
    //     //
    //     // return new AvailableTokensDto()
    //     // {
    //     //     TokenList = tokenList
    //     // };
    // }

    public async Task<AvailableTokensDto> GetAvailableTokensAsync()
    {
        var result = new AvailableTokensDto();
        var address = await GetUserAddressAsync();
        if (address.IsNullOrEmpty()) return result;
        var listDto = await _tokenInvokeProvider.GetUserTokenOwnerList(address);
        if (listDto == null || listDto.TokenOwnerList.IsNullOrEmpty()) return result;
        foreach (var token in listDto.TokenOwnerList)
        {
            result.TokenList.Add(new()
            {
                TokenName = token.TokenName,
                Symbol = token.Symbol,
                TokenImage = token.Icon,
                Holders = token.Holders,
                LiquidityInUsd = token.LiquidityInUsd
            });
        }

        return result;
    }

    public async Task<bool> CommitTokenAccessInfoAsync(UserTokenAccessInfoInput input)
    {
        var address = await GetUserAddressAsync();
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");
        AssertHelper.IsTrue(input.Email.Contains(CommonConstant.At), "Please enter a valid email address");
        var listDto = await _tokenInvokeProvider.GetAsync(address);

        AssertHelper.IsTrue(listDto != null && listDto.Exists(t => t.Symbol == input.Symbol) &&
                            CheckLiquidityAndHolderAvailable(listDto, input.Symbol), "Symbol invalid.");

        var dto = _objectMapper.Map<UserTokenAccessInfoInput, UserTokenAccessInfo>(input);
        dto.Address = address;
        await _userAccessTokenInfoRepository.InsertAsync(dto, autoSave: true);
        return true;
    }

    public async Task<UserTokenAccessInfoDto> GetUserTokenAccessInfoAsync(UserTokenAccessInfoBaseInput input)
    {
        var address = await GetUserAddressAsync();
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");
        var listDto = await _tokenInvokeProvider.GetAsync(address);
        AssertHelper.IsTrue(listDto != null && !listDto.IsNullOrEmpty() &&
                            listDto.Exists(t => t.Symbol == input.Symbol) &&
                            CheckLiquidityAndHolderAvailable(listDto, input.Symbol), "Symbol invalid.");

        return _objectMapper.Map<UserTokenAccessInfoIndex, UserTokenAccessInfoDto>(
            await GetUserTokenAccessInfoIndexAsync(input.Symbol, address));
    }

    public async Task<CheckChainAccessStatusResultDto> CheckChainAccessStatusAsync(CheckChainAccessStatusInput input)
    {
        var result = new CheckChainAccessStatusResultDto();
        var address = await GetUserAddressAsync();
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");
        var listDto = await _tokenInvokeProvider.GetAsync(address);
        AssertHelper.IsTrue(listDto != null && listDto.Exists(t => t.Symbol == input.Symbol) &&
                            CheckLiquidityAndHolderAvailable(listDto, input.Symbol), "Symbol invalid.");

        var networkList = _networkInfoOptions.NetworkMap.OrderBy(m =>
                _tokenOptions.Transfer.Select(t => t.Symbol).ToList().IndexOf(m.Key))
            .SelectMany(kvp => kvp.Value).Where(a =>
                a.SupportType.Contains(OrderTypeEnum.Transfer.ToString())).GroupBy(g => g.NetworkInfo.Network)
            .Select(s => s.First().NetworkInfo).ToList();

        result.ChainList.AddRange(networkList.Where(
            t => t.Network == ChainId.AELF || t.Network == ChainId.tDVV || t.Network == ChainId.tDVW).Select(
            t => new ChainAccessInfo { ChainId = t.Network, ChainName = t.Name, Symbol = input.Symbol }));
        result.OtherChainList.AddRange(networkList.Where(
            t => t.Network != ChainId.AELF && t.Network != ChainId.tDVV && t.Network != ChainId.tDVW).Select(
            t => new ChainAccessInfo { ChainId = t.Network, ChainName = t.Name, Symbol = input.Symbol }));

        // var tokenInvokeGrain = _clusterClient.GetGrain<ITokenInvokeGrain>(
        //     string.Join(CommonConstant.Underline, input.Symbol, address));
        // await tokenInvokeGrain.GetThirdTokenList(address, input.Symbol);
        // var applyOrderList = await GetTokenApplyOrderIndexListAsync(address, input.Symbol);

        await _tokenInvokeProvider.GetThirdTokenList(address, input.Symbol);
        var applyOrderList = await GetTokenApplyOrderIndexListAsync(address, input.Symbol);
        foreach (var item in result.ChainList)
        {
            var isCompleted = _tokenInfoOptions.ContainsKey(item.Symbol) &&
                              _tokenInfoOptions[item.Symbol].Transfer.Contains(item.ChainId);
            var tokenOwner = listDto.FirstOrDefault(t => t.Symbol == input.Symbol &&
                                                         t.ChainIds.Contains(item.ChainId));
            var applyStatus = applyOrderList.FirstOrDefault(t => t.OtherChainTokenInfo == null &&
                                                                 t.ChainTokenInfo.Exists(c =>
                                                                     c.ChainId == item.ChainId))?
                .ChainTokenInfo?.FirstOrDefault(c => c.ChainId == item.ChainId)?.Status;
            // var userTokenIssueGrain = _clusterClient.GetGrain<IUserTokenIssueGrain>(
            //     GuidHelper.UniqGuid(input.Symbol, address, item.ChainId));
            // var res = await userTokenIssueGrain.Get();

            var res = await _userTokenIssueRepository.FindAsync(o =>
                o.Address == address && o.OtherChainId == item.ChainId && o.Symbol == item.Symbol);
            item.TotalSupply = tokenOwner?.TotalSupply ?? 0;
            item.Decimals = tokenOwner?.Decimals ?? 0;
            item.TokenName = tokenOwner?.TokenName;
            item.ContractAddress = tokenOwner?.ContractAddress;
            item.Icon = tokenOwner?.Icon;
            item.Status = isCompleted
                ? TokenApplyOrderStatus.Complete.ToString()
                : !applyStatus.IsNullOrEmpty()
                    ? applyStatus
                    : res != null && !res.Status.IsNullOrEmpty()
                        ? res.Status
                        : tokenOwner?.Status ?? TokenApplyOrderStatus.Unissued.ToString();
            item.Checked = isCompleted ||
                           applyOrderList.Exists(t => !t.ChainTokenInfo.IsNullOrEmpty() &&
                                                      t.ChainTokenInfo.Exists(c => c.ChainId == item.ChainId));
            if (res != null && !res.BindingId.IsNullOrEmpty() && !res.ThirdTokenId.IsNullOrEmpty())
            {
                item.BindingId = res.BindingId;
                item.ThirdTokenId = res.ThirdTokenId;
            }
        }

        foreach (var item in result.OtherChainList)
        {
            var isCompleted = _tokenInfoOptions.ContainsKey(item.Symbol) &&
                              _tokenInfoOptions[item.Symbol].Transfer.Contains(item.ChainId);
            var applyStatus = applyOrderList.FirstOrDefault(t => t.OtherChainTokenInfo != null &&
                                                                 t.OtherChainTokenInfo.ChainId == item.ChainId)?.Status;
            // var userTokenIssueGrain = _clusterClient.GetGrain<IUserTokenIssueGrain>(
            //     GuidHelper.UniqGuid(input.Symbol, address, item.ChainId));
            // var res = await userTokenIssueGrain.Get();
            var res = await _userTokenIssueRepository.FindAsync(o =>
                o.Address == address && o.OtherChainId == item.ChainId && o.Symbol == item.Symbol);
            item.TotalSupply = res?.TotalSupply.SafeToDecimal() ?? 0M;
            item.Decimals = 0;
            item.TokenName = res?.TokenName;
            item.ContractAddress = res?.ContractAddress;
            item.Icon = res?.TokenImage;
            item.Status = isCompleted
                ? TokenApplyOrderStatus.Complete.ToString()
                : !applyStatus.IsNullOrEmpty()
                    ? applyStatus
                    : res != null && !res.Status.IsNullOrEmpty()
                        ? res.Status
                        : TokenApplyOrderStatus.Unissued.ToString();
            item.Checked = isCompleted ||
                           applyOrderList.Exists(t => t.OtherChainTokenInfo != null &&
                                                      t.OtherChainTokenInfo.ChainId == item.ChainId);
            if (res != null && !res.BindingId.IsNullOrEmpty() && !res.ThirdTokenId.IsNullOrEmpty())
            {
                item.BindingId = res.BindingId;
                item.ThirdTokenId = res.ThirdTokenId;
            }
        }

        return result;
    }

    public Task<AddChainResultDto> AddChainAsync(AddChainInput input)
    {
        throw new NotImplementedException();
    }

    public async Task<UserTokenBindingDto> PrepareBindingIssueAsync(PrepareBindIssueInput input)
    {
        AssertHelper.IsTrue(!input.ChainId.IsNullOrEmpty() || !input.OtherChainId.IsNullOrEmpty(),
            "Param invalid.");
        var chainStatus = await CheckChainAccessStatusAsync(new CheckChainAccessStatusInput { Symbol = input.Symbol });
        AssertHelper.IsTrue(input.ChainId.IsNullOrEmpty() || chainStatus.ChainList.Exists(
            c => c.ChainId == input.ChainId), "Param invalid.");
        AssertHelper.IsTrue(input.OtherChainId.IsNullOrEmpty() || chainStatus.OtherChainList.Exists(
            c => c.ChainId == input.OtherChainId), "Param invalid.");

        var address = await GetUserAddressAsync();
        // var tokenInvokeGrain = _clusterClient.GetGrain<ITokenInvokeGrain>(
        //     string.Join(CommonConstant.Underline, input.Symbol, address, input.OtherChainId));
        var dto = new UserTokenIssueDto
        {
            // Id = GuidHelper.UniqGuid(input.Symbol, address, input.OtherChainId),
            Address = address,
            WalletAddress = input.Address,
            Symbol = input.Symbol,
            ChainId = input.ChainId,
            TokenName = chainStatus.ChainList.FirstOrDefault(t => t.ChainId == input.ChainId).TokenName,
            TokenImage = chainStatus.ChainList.FirstOrDefault(t => t.ChainId == input.ChainId).Icon,
            OtherChainId = input.OtherChainId,
            ContractAddress = input.ContractAddress,
            TotalSupply = input.Supply
        };
        return await _tokenInvokeProvider.PrepareBinding(dto);
        // return await tokenInvokeGrain.PrepareBinding(dto);
    }

    public async Task<bool> GetBindingIssueAsync(UserTokenBindingDto input)
    {
        var address = await GetUserAddressAsync();
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");

        // var tokenInvokeGrain = _clusterClient.GetGrain<ITokenInvokeGrain>(
        //     string.Join(CommonConstant.Underline, input.BindingId, input.ThirdTokenId));
        // return await tokenInvokeGrain.Binding(input);
        return await _tokenInvokeProvider.Binding(input);
    }

    Task<PagedResultDto<TokenApplyOrderResultDto>> ITokenAccessAppService.GetTokenApplyOrderListAsync(
        GetTokenApplyOrderListInput input)
    {
        throw new NotImplementedException();
    }

    public Task<List<TokenApplyOrderResultDto>> GetTokenApplyOrderDetailAsync(GetTokenApplyOrderInput input)
    {
        throw new NotImplementedException();
    }

    // public async Task CommitTokenAccessInfoAsync(UserTokenAccessInfoInput input)
    // {
    //     var token = await _aElfClientProvider.GetTokenAsync(CrossChainServerConsts.AElfMainChainId, null, input.Symbol);
    //     if (token.Owner != input.Address)
    //     {
    //         throw new UserFriendlyException("No permission.");
    //     }
    //
    //     var userTokenAccessInfo =
    //         await _userAccessTokenInfoRepository.FirstOrDefaultAsync(t => t.Symbol == input.Symbol);
    //     if (userTokenAccessInfo == null)
    //     {
    //         await _userAccessTokenInfoRepository.InsertAsync(
    //             ObjectMapper.Map<UserTokenAccessInfoInput, UserTokenAccessInfo>(input));
    //     }
    //     else
    //     {
    //         await _userAccessTokenInfoRepository.UpdateAsync(
    //             ObjectMapper.Map<UserTokenAccessInfoInput, UserTokenAccessInfo>(input));
    //     }
    // }
    //
    // public async Task<UserTokenAccessInfoDto> GetUserTokenAccessInfoAsync(UserTokenAccessInfoInput input)
    // {
    //     return ObjectMapper.Map<UserTokenAccessInfoIndex, UserTokenAccessInfoDto>(
    //         await GetUserTokenAccessInfoIndexAsync(input.Symbol));
    // }
    //
    // private async Task<UserTokenAccessInfoIndex> GetUserTokenAccessInfoIndexAsync(string symbol)
    // {
    //     var mustQuery = new List<Func<QueryContainerDescriptor<UserTokenAccessInfoIndex>, QueryContainer>>();
    //     mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol)));
    //     QueryContainer Filter(QueryContainerDescriptor<UserTokenAccessInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
    //     return await _userAccessTokenInfoIndexRepository.GetAsync(Filter);
    // }

    // private async Task<List<TokenApplyOrderIndex>> GetTokenApplyOrderIndexListAsync(string address,
    //     string symbol = null, List<TokenApplyOrderStatus> statusList = null)
    // {
    //     var mustQuery = new List<Func<QueryContainerDescriptor<TokenApplyOrderIndex>, QueryContainer>>();
    //     mustQuery.Add(q => q.Term(i => i.Field(f => f.UserAddress).Value(address)));
    //     if (String.IsNullOrWhiteSpace(symbol))
    //     {
    //         mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol)));
    //     }
    //
    //     if (!statusList.IsNullOrEmpty())
    //     {
    //         mustQuery.Add(q => q.Terms(i => i.Field(f => f.Status).Terms(statusList)));
    //     }
    //
    //     QueryContainer Filter(QueryContainerDescriptor<TokenApplyOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
    //     var result = await _tokenApplyOrderIndexRepository.GetListAsync(Filter);
    //     return result.Item2;
    // }

    // public async Task<CheckChainAccessStatusResultDto> CheckChainAccessStatusAsync(string symbol, string address)
    // {
    //     var result = new CheckChainAccessStatusResultDto();
    //     foreach (var chainId in _tokenAccessOptions.ChainIdList)
    //     {
    //         result.ChainList.Add(new ChainAccessInfo
    //         {
    //             ChainId = chainId,
    //         });
    //     }
    //
    //     foreach (var otherChainId in _tokenAccessOptions.ChainIdList)
    //     {
    //         result.OtherChainList.Add(new ChainAccessInfo
    //         {
    //             ChainId = otherChainId,
    //         });
    //     }
    //
    //     var userAccessTokenInfoIndex = await GetUserTokenAccessInfoIndexAsync(symbol);
    //     if (userAccessTokenInfoIndex != null)
    //     {
    //         var chainIds = JsonSerializer.Deserialize<List<string>>(userAccessTokenInfoIndex.ChainIds);
    //         var otherChainIds = JsonSerializer.Deserialize<List<string>>(userAccessTokenInfoIndex.OtherChainIds);
    //         foreach (var chainId in chainIds)
    //         {
    //             result.ChainList.First(t => t.ChainId == chainId).Status = ChainAccessStatus.Accessed;
    //         }
    //
    //         foreach (var otherChainId in otherChainIds)
    //         {
    //             result.OtherChainList.First(t => t.ChainId == otherChainId).Status = ChainAccessStatus.Accessed;
    //         }
    //     }
    //
    //     var applyOrderList = await GetTokenApplyOrderIndexListAsync(address, symbol,
    //         new List<TokenApplyOrderStatus>
    //             { TokenApplyOrderStatus.PoolInitializing, TokenApplyOrderStatus.AddLiquidity });
    //     foreach (var applyOrderIndex in applyOrderList)
    //     {
    //         var chainIds = JsonSerializer.Deserialize<List<string>>(applyOrderIndex.ChainIds);
    //         foreach (var chainId in chainIds)
    //         {
    //             result.ChainList.First(t => t.ChainId == chainId).Status = ChainAccessStatus.Accessing;
    //         }
    //
    //         result.OtherChainList.First(t => t.ChainId == applyOrderIndex.OtherChainId).Status =
    //             ChainAccessStatus.Accessing;
    //     }
    //
    //     var unissuedChainList = result.OtherChainList.Where(t => t.Status == ChainAccessStatus.Unissued).ToList();
    //     var issueChainList = await _symbolMarketProvider.GetIssueChainListAsync(symbol);
    //     foreach (var otherChainInfo in unissuedChainList)
    //     {
    //         if (issueChainList.Contains(otherChainInfo.ChainId))
    //         {
    //             otherChainInfo.Status = ChainAccessStatus.Issued;
    //         }
    //     }
    //
    //     return result;
    // }
    //
    // public async Task SelectChainAsync(SelectChainInput input)
    // {
    //     foreach (var chainId in input.ChainIds)
    //     {
    //         if (!_tokenAccessOptions.ChainIdList.Contains(chainId))
    //         {
    //             throw new UserFriendlyException("invalid chainId");
    //         }
    //     }
    //
    //     foreach (var otherChainId in input.OtherChainIds)
    //     {
    //         if (!_tokenAccessOptions.OtherChainIdList.Contains(otherChainId))
    //         {
    //             throw new UserFriendlyException("invalid otherChainId");
    //         }
    //     }
    //
    //     var userAccessTokenInfo = await _userAccessTokenInfoRepository.GetAsync(t => t.Symbol == input.Symbol);
    //     if (userAccessTokenInfo.Address != input.Address)
    //     {
    //         throw new UserFriendlyException("No permission.");
    //     }
    //
    //     var addChainIds = input.ChainIds.Except(JsonSerializer.Deserialize<List<string>>(userAccessTokenInfo.ChainIds))
    //         .ToList();
    //     var addOtherChainIds = input.OtherChainIds
    //         .Except(JsonSerializer.Deserialize<List<string>>(userAccessTokenInfo.OtherChainIds)).ToList();
    //     if (addChainIds.IsNullOrEmpty() && addOtherChainIds.IsNullOrEmpty())
    //     {
    //         return;
    //     }
    //
    //     // split order
    //     var applyOrderList = new List<TokenApplyOrder>();
    //     var firstApplyOrder = new TokenApplyOrder
    //     {
    //         Symbol = userAccessTokenInfo.Symbol,
    //         UserAddress = userAccessTokenInfo.Address,
    //         ChainIds = JsonSerializer.Serialize(addChainIds),
    //     };
    //     if (addOtherChainIds.Count > 0)
    //     {
    //         firstApplyOrder.OtherChainId = addOtherChainIds[0];
    //         addOtherChainIds.RemoveAt(0);
    //     }
    //
    //     applyOrderList.Add(firstApplyOrder);
    //     foreach (var otherChainId in addOtherChainIds)
    //     {
    //         applyOrderList.Add(new TokenApplyOrder()
    //         {
    //             Symbol = userAccessTokenInfo.Symbol,
    //             UserAddress = userAccessTokenInfo.Address,
    //             OtherChainId = otherChainId
    //         });
    //     }
    //
    //     await _tokenApplyOrderRepository.InsertManyAsync(applyOrderList);
    //     await _larkManager.SendMessageAsync("");
    // }
    //
    // public async Task IssueTokenAsync(IssueTokenInput input)
    // {
    //     await _symbolMarketProvider.IssueTokenAsync(input);
    // }
    //
    // public async Task<TokenApplyOrderListDto> GetTokenApplyOrderListAsync(GetTokenApplyOrderListInput input)
    // {
    //     var mustQuery = new List<Func<QueryContainerDescriptor<TokenApplyOrderIndex>, QueryContainer>>();
    //     mustQuery.Add(q => q.Term(i => i.Field(f => f.UserAddress).Value(input.Address)));
    //     QueryContainer Filter(QueryContainerDescriptor<TokenApplyOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
    //     var result = await _tokenApplyOrderIndexRepository.GetListAsync(Filter, sortExp: o => o.UpdateTime,
    //         sortType: SortOrder.Descending, skip: input.SkipCount, limit: input.MaxResultCount);
    //     var totalCount = await _tokenApplyOrderIndexRepository.CountAsync(Filter);
    //     return new TokenApplyOrderListDto()
    //     {
    //         Items = ObjectMapper.Map<List<TokenApplyOrderIndex>, List<TokenApplyOrderDto>>(result.Item2),
    //         TotalCount = totalCount.Count
    //     };
    // }
    //
    // public async Task<TokenApplyOrderDto> GetTokenApplyOrderAsync(Guid id)
    // {
    //     var tokenApplyOrder = await _tokenApplyOrderIndexRepository.GetAsync(id);
    //     return ObjectMapper.Map<TokenApplyOrderIndex, TokenApplyOrderDto>(tokenApplyOrder);
    // }

    public async Task AddUserTokenAccessInfoIndexAsync(AddUserTokenAccessInfoIndexInput input)
    {
        var index = ObjectMapper.Map<AddUserTokenAccessInfoIndexInput, UserTokenAccessInfoIndex>(input);
        await _userAccessTokenInfoIndexRepository.AddAsync(index);
    }

    public async Task UpdateUserTokenAccessInfoIndexAsync(UpdateUserTokenAccessInfoIndexInput input)
    {
        var index = ObjectMapper.Map<UpdateUserTokenAccessInfoIndexInput, UserTokenAccessInfoIndex>(input);
        await _userAccessTokenInfoIndexRepository.UpdateAsync(index);
    }

    public async Task DeleteUserTokenAccessInfoIndexAsync(DeleteUserTokenAccessInfoIndexInput input)
    {
        var index = ObjectMapper.Map<DeleteUserTokenAccessInfoIndexInput, UserTokenAccessInfoIndex>(input);
        await _userAccessTokenInfoIndexRepository.DeleteAsync(index);
    }

    public async Task AddTokenApplyOrderIndexAsync(AddTokenApplyOrderIndexInput input)
    {
        var index = ObjectMapper.Map<AddTokenApplyOrderIndexInput, TokenApplyOrderIndex>(input);
        await _tokenApplyOrderIndexRepository.AddAsync(index);
    }

    public async Task UpdateTokenApplyOrderIndexAsync(UpdateTokenApplyOrderIndexInput input)
    {
        var index = ObjectMapper.Map<UpdateTokenApplyOrderIndexInput, TokenApplyOrderIndex>(input);
        await _tokenApplyOrderIndexRepository.UpdateAsync(index);
    }

    public async Task<TokenConfigDto> GetTokenConfigAsync(GetTokenConfigInput input)
    {
        return new TokenConfigDto
        {
            LiquidityInUsd = !_tokenAccessOptions.TokenConfig.ContainsKey(input.Symbol)
                ? _tokenAccessOptions.DefaultConfig.Liquidity
                : _tokenAccessOptions.TokenConfig[input.Symbol].Liquidity,
            Holders = !_tokenAccessOptions.TokenConfig.ContainsKey(input.Symbol)
                ? _tokenAccessOptions.DefaultConfig.Holders
                : _tokenAccessOptions.TokenConfig[input.Symbol].Holders
        };
    }

    public Task<TokenWhitelistDto> GetTokenWhitelistAsync()
    {
        var tokenWhitelist = _tokenWhitelistOptions.TokenWhitelist;
        var result = new TokenWhitelistDto
        {
            Data = tokenWhitelist.ToDictionary(
                tokenEntry => tokenEntry.Key,
                tokenEntry => tokenEntry.Value.ToDictionary(
                    chainEntry => chainEntry.Key,
                    chainEntry => ObjectMapper.Map<TokenInfo, TokenInfoDto>(chainEntry.Value)
                )
            )
        };
        return Task.FromResult(result);
    }

    public async Task<PoolOverviewDto> GetPoolOverviewAsync(string address)
    {
        var tokenCount = _tokenWhitelistOptions.TokenWhitelist.Count;
        var poolCount = _tokenWhitelistOptions.TokenWhitelist.Values.Sum(i => i.Count);
        var poolLiquidityInfoList = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(
            new GetPoolLiquidityInfosInput
            {
                MaxResultCount = MaxMaxResultCount,
                SkipCount = DefaultSkipCount
            });
        var userLiquidityInfoList = string.IsNullOrWhiteSpace(address)
            ? new List<UserLiquidityIndexDto>()
            : await _userLiquidityInfoAppService.GetUserLiquidityInfosAsync(new GetUserLiquidityInput
                { Provider = address });
        var tokenPrice = await GetTokenPricesAsync(poolLiquidityInfoList.Items.ToList());
        var totalLiquidityInUsd = CalculateTotalLiquidityInUsd(poolLiquidityInfoList.Items.ToList(), tokenPrice);
        var totalMyLiquidityInUsd = CalculateUserTotalLiquidityInUsd(userLiquidityInfoList, tokenPrice);
        return new PoolOverviewDto
        {
            MyTotalTvlInUsd = totalMyLiquidityInUsd,
            TotalTvlInUsd = totalLiquidityInUsd,
            PoolCount = poolCount,
            TokenCount = tokenCount
        };
    }

    public async Task<PagedResultDto<PoolInfoDto>> GetPoolListAsync(GetPoolListInput input)
    {
        var poolLiquidityInfos = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(
            new GetPoolLiquidityInfosInput
            {
                MaxResultCount = input.MaxResultCount,
                SkipCount = input.SkipCount
            });
        var poolLiquidityInfoList = poolLiquidityInfos.Items.ToList();
        var result = new List<PoolInfoDto>();
        var tokenPrice = await GetTokenPricesAsync(poolLiquidityInfoList);
        var groupedPoolLiquidityInfoList = poolLiquidityInfoList.GroupBy(p => p.ChainId);

        foreach (var group in groupedPoolLiquidityInfoList)
        {
            var chainId = group.Key;
            var chain = await _chainAppService.GetAsync(chainId);
            var userLiquidityInfo = await _userLiquidityInfoAppService.GetUserLiquidityInfosAsync(
                new GetUserLiquidityInput
                {
                    ChainId = chainId
                });
            foreach (var poolLiquidity in group)
            {
                var tokenPriceInUsd = tokenPrice[poolLiquidity.TokenInfo.Symbol];
                var poolInfo = new PoolInfoDto
                {
                    ChainId = poolLiquidity.ChainId,
                    Token = poolLiquidity.TokenInfo,
                    TotalTvlInUsd = poolLiquidity.Liquidity * tokenPriceInUsd,
                    TokenPrice = tokenPriceInUsd
                };
                var userLiquidity = chain.Type == BlockchainType.AElf
                    ? userLiquidityInfo.FirstOrDefault(u => u.TokenInfo.Symbol == poolLiquidity.TokenInfo.Symbol)
                    : userLiquidityInfo.FirstOrDefault(u => u.TokenInfo.Address == poolLiquidity.TokenInfo.Address);
                poolInfo.MyTvlInUsd = userLiquidity?.Liquidity * tokenPriceInUsd ?? 0;
                result.Add(poolInfo);
            }
        }

        return new PagedResultDto<PoolInfoDto>
        {
            TotalCount = poolLiquidityInfos.TotalCount,
            Items = result.OrderByDescending(r => r.TotalTvlInUsd).ToList()
        };
    }

    public async Task<PoolInfoDto> GetPoolDetailAsync(GetPoolDetailInput input)
    {
        var poolLiquidityInfos = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(
            new GetPoolLiquidityInfosInput
            {
                ChainId = input.ChainId,
                Token = input.Token
            });
        var poolLiquidity = poolLiquidityInfos.Items.FirstOrDefault();
        if (poolLiquidity == null)
        {
            Log.Warning("Pool liquidity info not found.{chainId} {token}", input.ChainId, input.Token);
            return new PoolInfoDto();
        }

        var tokenSymbol = (await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = input.ChainId,
            Address = input.Token
        })).Symbol;
        var coinId = _tokenPriceIdMappingOptions.CoinIdMapping[tokenSymbol];
        var priceInUsd = await _tokenPriceProvider.GetPriceAsync(coinId);
        var myLiquidity = 0m;
        if (!string.IsNullOrWhiteSpace(input.Address))
        {
            var userLiq = await _userLiquidityInfoAppService.GetUserLiquidityInfosAsync(new GetUserLiquidityInput
            {
                Provider = input.Address,
                ChainId = input.ChainId,
                Token = input.Token
            });
            myLiquidity = userLiq.FirstOrDefault()?.Liquidity ?? 0;
        }

        return new PoolInfoDto
        {
            ChainId = poolLiquidity.ChainId,
            Token = poolLiquidity.TokenInfo,
            TotalTvlInUsd = poolLiquidity.Liquidity * priceInUsd,
            MyTvlInUsd = myLiquidity * priceInUsd,
            TokenPrice = priceInUsd
        };
    }

    private async Task<Dictionary<string, decimal>> GetTokenPricesAsync(
        List<PoolLiquidityIndexDto> poolLiquidityInfoList)
    {
        var allSymbols = poolLiquidityInfoList
            .Select(pool => pool.TokenInfo.Symbol)
            .Distinct()
            .ToList();
        var tokenPrices = new Dictionary<string, decimal>();
        foreach (var symbol in allSymbols)
        {
            var coinId = _tokenPriceIdMappingOptions.CoinIdMapping[symbol];
            var priceInUsd = await _tokenPriceProvider.GetPriceAsync(coinId);
            // var priceInUsd = 1000;
            tokenPrices[symbol] = priceInUsd;
        }

        return tokenPrices;
    }

    private decimal CalculateTotalLiquidityInUsd(
        List<PoolLiquidityIndexDto> poolLiquidityInfoList,
        Dictionary<string, decimal> tokenPrices)
    {
        if (poolLiquidityInfoList.Count == 0)
        {
            return 0m;
        }

        return poolLiquidityInfoList
            .GroupBy(pool => pool.TokenInfo.Symbol)
            .Sum(group =>
            {
                var priceInUsd = tokenPrices[group.Key];
                var totalLiquidity = group.Sum(pool => pool.Liquidity);
                return totalLiquidity * priceInUsd;
            });
    }

    private decimal CalculateUserTotalLiquidityInUsd(
        List<UserLiquidityIndexDto> userLiquidityInfoList,
        Dictionary<string, decimal> tokenPrices)
    {
        if (userLiquidityInfoList.Count == 0)
        {
            return 0m;
        }

        return userLiquidityInfoList
            .GroupBy(userLiq => userLiq.TokenInfo.Symbol)
            .Sum(group =>
            {
                var priceInUsd = tokenPrices[group.Key];
                var totalLiquidity = group.Sum(userLiq => userLiq.Liquidity);
                return totalLiquidity * priceInUsd;
            });
    }

    private async Task<string> GetUserAddressAsync()
    {
        var userId = CurrentUser.IsAuthenticated ? CurrentUser?.GetId() : null;
        if (!userId.HasValue) return null;
        var userDto = await _crossChainUserRepository.FindAsync(o => o.UserId == userId);
        return userDto?.AddressInfos?.FirstOrDefault()?.Address;
    }

    private bool CheckLiquidityAndHolderAvailable(List<TokenOwnerDto> TokenOwnerList, string symbol)
    {
        return true;
        // var tokenOwnerDto = TokenOwnerList.FirstOrDefault(t => t.Symbol == symbol);
        // var liquidityInUsd = !_tokenAccessOptions.TokenConfig.ContainsKey(symbol)
        //     ? _tokenAccessOptions.DefaultConfig.Liquidity
        //     : _tokenAccessOptions.TokenConfig[symbol].Liquidity;
        // var holders = !_tokenAccessOptions.TokenConfig.ContainsKey(symbol)
        //     ? _tokenAccessOptions.DefaultConfig.Holders
        //     : _tokenAccessOptions.TokenConfig[symbol].Holders;
        // return tokenOwnerDto.LiquidityInUsd.SafeToDecimal() > liquidityInUsd.SafeToDecimal()
        //        && tokenOwnerDto.Holders > holders;
    }

    private async Task<UserTokenAccessInfoIndex> GetUserTokenAccessInfoIndexAsync(string symbol, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserTokenAccessInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));
        QueryContainer Filter(QueryContainerDescriptor<UserTokenAccessInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _userAccessTokenInfoIndexRepository.GetAsync(Filter);
    }

    private async Task<List<TokenApplyOrderIndex>> GetTokenApplyOrderIndexListAsync(string address, string symbol,
        string id = null, string chainId = null)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TokenApplyOrderIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserAddress).Value(address)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol)));
        if (!id.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(id)));
        }

        if (!chainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Bool(i => i.Should(
                s => s.Match(k =>
                    k.Field("chainTokenInfo.chainId").Query(chainId)),
                s => s.Term(k =>
                    k.Field(f => f.OtherChainTokenInfo.ChainId).Value(chainId)))));
        }

        QueryContainer Filter(QueryContainerDescriptor<TokenApplyOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _tokenApplyOrderIndexRepository.GetListAsync(Filter);
        return result.Item2;
    }
}