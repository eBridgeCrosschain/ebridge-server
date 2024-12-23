using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Notify;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.TokenPrice;
using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Users;

namespace AElf.CrossChainServer.TokenAccess;

[RemoteService(IsEnabled = false)]
public class TokenAccessAppService : CrossChainServerAppService, ITokenAccessAppService
{
    private readonly ITokenApplyOrderRepository _tokenApplyOrderRepository;
    private readonly IUserAccessTokenInfoRepository _userAccessTokenInfoRepository;
    private readonly IUserTokenIssueRepository _userTokenIssueRepository;
    private readonly ICrossChainUserRepository _crossChainUserRepository;
    private readonly INESTRepository<TokenApplyOrderIndex, Guid> _tokenApplyOrderIndexRepository;
    private readonly INESTRepository<UserTokenAccessInfoIndex, Guid> _userAccessTokenInfoIndexRepository;

    private readonly IPoolLiquidityInfoAppService _poolLiquidityInfoAppService;
    private readonly IUserLiquidityInfoAppService _userLiquidityInfoAppService;
    private readonly IChainAppService _chainAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly IBridgeContractAppService _bridgeContractAppService;

    private readonly ILarkRobotNotifyProvider _larkRobotNotifyProvider;
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly IIndexerCrossChainLimitInfoService _indexerCrossChainLimitInfoService;
    private readonly ITokenInvokeProvider _tokenInvokeProvider;

    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly TokenWhitelistOptions _tokenWhitelistOptions;
    private readonly TokenPriceIdMappingOptions _tokenPriceIdMappingOptions;

    private const int MaxMaxResultCount = 1000;
    private const int DefaultSkipCount = 0;
    private const string TokenListingAlarm = "TokenListingAlarm";

    public TokenAccessAppService(
        ITokenApplyOrderRepository tokenApplyOrderRepository,
        INESTRepository<TokenApplyOrderIndex, Guid> tokenApplyOrderIndexRepository,
        IUserAccessTokenInfoRepository userAccessTokenInfoRepository,
        IUserTokenIssueRepository userTokenIssueRepository,
        INESTRepository<UserTokenAccessInfoIndex, Guid> userAccessTokenInfoIndexRepository,
        ILarkRobotNotifyProvider larkRobotNotifyProvider, IPoolLiquidityInfoAppService poolLiquidityInfoAppService,
        IUserLiquidityInfoAppService userLiquidityInfoAppService, ITokenPriceProvider tokenPriceProvider,
        IChainAppService chainAppService, ITokenAppService tokenAppService,
        ICrossChainUserRepository crossChainUserRepository, IBridgeContractAppService bridgeContractAppService,
        IIndexerCrossChainLimitInfoService indexerCrossChainLimitInfoService, ITokenInvokeProvider tokenInvokeProvider,
        IOptionsSnapshot<TokenOptions> tokenOptions, IOptionsSnapshot<TokenInfoOptions> tokenInfoOptions,
        IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions,
        IOptionsSnapshot<TokenWhitelistOptions> tokenWhitelistOptions,
        IOptionsSnapshot<TokenPriceIdMappingOptions> tokenPriceIdMappingOptions)
    {
        _tokenApplyOrderRepository = tokenApplyOrderRepository;
        _tokenApplyOrderIndexRepository = tokenApplyOrderIndexRepository;
        _userAccessTokenInfoRepository = userAccessTokenInfoRepository;
        _userTokenIssueRepository = userTokenIssueRepository;
        _userAccessTokenInfoIndexRepository = userAccessTokenInfoIndexRepository;
        _larkRobotNotifyProvider = larkRobotNotifyProvider;
        _poolLiquidityInfoAppService = poolLiquidityInfoAppService;
        _userLiquidityInfoAppService = userLiquidityInfoAppService;
        _tokenPriceProvider = tokenPriceProvider;
        _chainAppService = chainAppService;
        _tokenAppService = tokenAppService;
        _crossChainUserRepository = crossChainUserRepository;
        _bridgeContractAppService = bridgeContractAppService;
        _indexerCrossChainLimitInfoService = indexerCrossChainLimitInfoService;
        _tokenInvokeProvider = tokenInvokeProvider;
        _tokenAccessOptions = tokenAccessOptions.Value;
        _tokenWhitelistOptions = tokenWhitelistOptions.Value;
        _tokenPriceIdMappingOptions = tokenPriceIdMappingOptions.Value;
    }

    public async Task<AvailableTokensDto> GetAvailableTokensAsync()
    {
        var result = new AvailableTokensDto();
        // var address = await GetUserAddressAsync();
        var address = " ";
        if (address.IsNullOrEmpty()) return result;
        var listDto = await _tokenInvokeProvider.GetUserTokenOwnerListAsync(address);
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
        // var address = await GetUserAddressAsync();
        var address = " ";
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");
        AssertHelper.IsTrue(input.Email.Contains(CommonConstant.At), "Please enter a valid email address");
        var listDto = await _tokenInvokeProvider.GetAsync(address);

        AssertHelper.IsTrue(listDto != null && listDto.Exists(t => t.Symbol == input.Symbol) &&
                            CheckLiquidityAndHolderAvailable(listDto, input.Symbol), "Symbol invalid.");

        var dto = ObjectMapper.Map<UserTokenAccessInfoInput, UserTokenAccessInfo>(input);
        dto.Address = address;
        var existDto = await _userAccessTokenInfoRepository.FindAsync(o => o.Address == address);
        if (existDto != null)
        {
            await _userAccessTokenInfoRepository.UpdateAsync(dto, autoSave: true);
        }
        else
        {
            await _userAccessTokenInfoRepository.InsertAsync(dto, autoSave: true);
        }

        return true;
    }

    public async Task<UserTokenAccessInfoDto> GetUserTokenAccessInfoAsync(UserTokenAccessInfoBaseInput input)
    {
        // var address = await GetUserAddressAsync();
        var address = " ";
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");
        var listDto = await _tokenInvokeProvider.GetAsync(address);
        AssertHelper.IsTrue(listDto != null && !listDto.IsNullOrEmpty() &&
                            listDto.Exists(t => t.Symbol == input.Symbol) &&
                            CheckLiquidityAndHolderAvailable(listDto, input.Symbol), "Symbol invalid.");

        return ObjectMapper.Map<UserTokenAccessInfoIndex, UserTokenAccessInfoDto>(
            await GetUserTokenAccessInfoIndexAsync(input.Symbol, address));
    }

    #region CheckChainAccessStatus

    public async Task<CheckChainAccessStatusResultDto> CheckChainAccessStatusAsync(CheckChainAccessStatusInput input)
    {
        var result = new CheckChainAccessStatusResultDto();
        // var address = await GetUserAddressAsync();
        var address = " ";
        // Validate user address
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");

        // Get token information for the user
        var listDto = await _tokenInvokeProvider.GetAsync(address);
        AssertHelper.IsTrue(
            listDto != null &&
            listDto.Exists(t => t.Symbol == input.Symbol) &&
            CheckLiquidityAndHolderAvailable(listDto, input.Symbol),
            "Symbol invalid."
        );

        // Populate chain and other chain lists
        PopulateChainLists(result, input.Symbol);

        // Update third-party token data
        await _tokenInvokeProvider.GetThirdTokenListAndUpdateAsync(address, input.Symbol);

        // Retrieve token apply order data
        var applyOrderList = await GetTokenApplyOrderIndexListAsync(address, input.Symbol);

        // Process chain list
        await ProcessChainListAsync(result.ChainList, listDto, applyOrderList, input.Symbol, address);

        // Process other chain list
        await ProcessOtherChainListAsync(result.OtherChainList, listDto, applyOrderList, input.Symbol, address);

        return result;
    }

    // Populates chain and other chain lists
    private void PopulateChainLists(CheckChainAccessStatusResultDto result, string symbol)
    {
        result.ChainList.AddRange(_tokenAccessOptions.ChainIdList.Select(c => new ChainAccessInfo
        {
            ChainId = c,
            ChainName = c,
            Symbol = symbol
        }));

        result.OtherChainList.AddRange(_tokenAccessOptions.OtherChainIdList.Select(c => new ChainAccessInfo
        {
            ChainId = c,
            ChainName = c,
            Symbol = symbol
        }));
    }

    // Processes the chain list to determine the status of each chain
    private async Task ProcessChainListAsync(
        List<ChainAccessInfo> chainList,
        List<TokenOwnerDto> listDto,
        List<TokenApplyOrderIndex> applyOrderList,
        string symbol,
        string address)
    {
        foreach (var item in chainList)
        {
            // Check if the token is completed on the chain
            var isCompleted = _tokenWhitelistOptions.TokenWhitelist.ContainsKey(symbol) &&
                              _tokenWhitelistOptions.TokenWhitelist[symbol].ContainsKey(item.ChainId);

            // Find the token details for the current chain
            var tokenOwner = listDto?.FirstOrDefault(t => t.Symbol == symbol && t.ChainIds.Contains(item.ChainId));
            // Get the status from the apply order list
            var applyStatus = applyOrderList.FirstOrDefault(t => !t.ChainTokenInfo.IsNullOrEmpty() &&
                                                                 t.ChainTokenInfo.Exists(c =>
                                                                     c.ChainId == item.ChainId))?
                .ChainTokenInfo?.FirstOrDefault(c => c.ChainId == item.ChainId)?.Status;
            // Retrieve user token issue details
            var res = await _userTokenIssueRepository.FindAsync(o =>
                o.Address == address && o.ChainId == item.ChainId && o.Symbol == item.Symbol);

            item.TotalSupply = tokenOwner?.TotalSupply ?? 0;
            item.Decimals = tokenOwner?.Decimals ?? 0;
            item.TokenName = tokenOwner?.TokenName;
            item.ContractAddress = tokenOwner?.ContractAddress;
            item.Icon = tokenOwner?.Icon;

            // Determine the status
            item.Status = DetermineStatus(isCompleted, applyStatus, res, tokenOwner);

            // Determine if the token is selected
            item.Checked = DetermineCheckedStatus(isCompleted, applyOrderList, item.ChainId);

            // Add binding and third token IDs if available
            if (res != null && !res.BindingId.IsNullOrEmpty() && !res.ThirdTokenId.IsNullOrEmpty())
            {
                item.BindingId = res.BindingId;
                item.ThirdTokenId = res.ThirdTokenId;
            }
        }
    }

    // Processes the chain list to determine the status of each chain
    private async Task ProcessOtherChainListAsync(
        List<ChainAccessInfo> otherChainList,
        List<TokenOwnerDto> listDto,
        List<TokenApplyOrderIndex> applyOrderList,
        string symbol,
        string address)
    {
        foreach (var item in otherChainList)
        {
            // Check if the token is completed on the chain
            var isCompleted = _tokenWhitelistOptions.TokenWhitelist.ContainsKey(symbol) &&
                              _tokenWhitelistOptions.TokenWhitelist[symbol].ContainsKey(item.ChainId);

            // Find the token details for the current chain
            var tokenOwner = listDto?.FirstOrDefault(t => t.Symbol == symbol && !t.TokenName.IsNullOrEmpty());
            // Get the status from the apply order list
            var applyOrder = applyOrderList.FirstOrDefault(t => t.OtherChainTokenInfo != null &&
                                                                t.OtherChainTokenInfo.ChainId == item.ChainId);
            var applyStatus = applyOrder?.OtherChainTokenInfo?.Status;
            // Retrieve user token issue details
            var res = await _userTokenIssueRepository.FindAsync(o =>
                o.Address == address && o.OtherChainId == item.ChainId && o.Symbol == item.Symbol);

            item.TotalSupply = res?.TotalSupply.SafeToDecimal() ?? 0M;
            item.Decimals = 0;
            item.TokenName = res?.TokenName ?? tokenOwner?.TokenName;
            item.ContractAddress = res?.ContractAddress;
            item.Icon = res?.TokenImage ?? tokenOwner?.Icon;

            // Determine the status
            item.Status = DetermineStatus(isCompleted, applyStatus, res, tokenOwner);

            // Determine if the token is selected
            item.Checked = DetermineCheckedStatus(isCompleted, applyOrderList, item.ChainId);

            // Add binding and third token IDs if available
            if (res != null && !res.BindingId.IsNullOrEmpty() && !res.ThirdTokenId.IsNullOrEmpty())
            {
                item.BindingId = res.BindingId;
                item.ThirdTokenId = res.ThirdTokenId;
            }
        }
    }

    // Determines the status of the token
    private static string DetermineStatus(bool isCompleted, string applyStatus, UserTokenIssueDto res,
        TokenOwnerDto tokenOwner)
    {
        if (isCompleted)
        {
            return TokenApplyOrderStatus.Complete.ToString();
        }

        if (!applyStatus.IsNullOrEmpty())
        {
            return applyStatus;
        }

        if (res != null && !res.Status.IsNullOrEmpty())
        {
            return res.Status;
        }

        return tokenOwner?.Status ?? TokenApplyOrderStatus.Unissued.ToString();
    }

    // Determines if the token is selected
    private static bool DetermineCheckedStatus(bool isCompleted, List<TokenApplyOrderIndex> applyOrderList,
        string chainId)
    {
        return isCompleted ||
               applyOrderList.Exists(t =>
                   t.ChainTokenInfo?.Any(c => c.ChainId == chainId) == true ||
                   t.OtherChainTokenInfo?.ChainId == chainId);
    }

    #endregion

    #region AddChain

    /// <summary>
    ///  1. When the input contains both ChainId and OtherChainId:
    ///     The order will be created following the OtherChainId, and the ChainId information from the input will be stored in the Order's ChainIdInfo.  
    ///     If the ChainId found in chainAccessStatus is not in the "issued" state, it means that the AElf chain's information has already been linked to an order of another third-party chain and cannot be bound again.  
    /// 2. When the input contains only ChainId (OtherChainId is null):  
    ///     If the ChainId found in chainAccessStatus is in the "issued" state and there is no existing order containing this ChainId,  
    ///     create a new Order with only the ChainId, leaving OtherChainId empty, and set its status to "poolInitialized," allowing liquidity to be added directly.  
    /// 3. When the input contains only OtherChainId (ChainId is null):  
    ///     Create a new Order using the OtherChainId.  
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public async Task<AddChainResultDto> AddChainAsync(AddChainInput input)
    {
        // var address = await GetUserAddressAsync();
        var address = " ";
        if (address.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Invalid address.");
        }

        var userAccessInfo =
            await _userAccessTokenInfoRepository.FindAsync(t => t.Symbol == input.Symbol && t.Address == address);

        // 1. Check chain access status - Get the status of my token on all chains
        var chainAccessStatus = await CheckChainAccessStatusAsync(new CheckChainAccessStatusInput
        {
            Symbol = input.Symbol
        });

        // Validate provided chain IDs against available chain access status
        ValidateChainIds(input.ChainIds, chainAccessStatus.ChainList, "Failed to add chain.");
        ValidateChainIds(input.OtherChainIds, chainAccessStatus.OtherChainList, "Failed to add chain.");

        var currentUserTokenApplyOrderCount = await GetTokenApplyOrderIndexListCountAsync(address, input.Symbol);
        if (currentUserTokenApplyOrderCount == 0)
        {
            // First apply order must include both ChainIds and OtherChainIds
            if (IsNullOrEmpty(input.ChainIds) || IsNullOrEmpty(input.OtherChainIds))
            {
                throw new UserFriendlyException("Failed to add chain.");
            }
        }

        // Additional validation for chains if one set of IDs is provided
        if (IsNonEmpty(input.ChainIds) && IsNullOrEmpty(input.OtherChainIds))
        {
            // Ensure no active pool initialization is in progress
            if (chainAccessStatus.ChainList.Any(t =>
                    t.Status == TokenApplyOrderStatus.PoolInitializing.ToString() ||
                    t.Status == TokenApplyOrderStatus.PoolInitialized.ToString()))
            {
                throw new UserFriendlyException(
                    "A listing is in progress. Wait for completion before adding the AElf chain.");
            }
        }

        var result = new AddChainResultDto();
        // 2. Process other chain
        if (IsNonEmpty(input.OtherChainIds))
        {
            foreach (var otherChainId in input.OtherChainIds)
            {
                await ProcessOtherChainAsync(otherChainId, input.Symbol, address, userAccessInfo?.OfficialWebsite,
                    chainAccessStatus.ChainList, chainAccessStatus.OtherChainList, input.ChainIds,
                    result);
            }
        }

        // 3. Process chain
        if (IsNonEmpty(input.ChainIds))
        {
            foreach (var chainId in input.ChainIds)
            {
                await ProcessChainAsync(chainId, input.Symbol, address, userAccessInfo?.OfficialWebsite,
                    chainAccessStatus.ChainList,
                    input.OtherChainIds, result);
            }
        }

        return result;
    }

    private void ValidateChainIds(List<string> chainIds, List<ChainAccessInfo> chainAccessList, string errorMessage)
    {
        if (IsNonEmpty(chainIds) && !chainIds.Any(t => chainAccessList.Exists(c => c.ChainId == t)))
        {
            throw new UserFriendlyException(errorMessage);
        }
    }

    private bool IsNullOrEmpty<T>(List<T> list)
    {
        return list == null || list.Count == 0;
    }

    private bool IsNonEmpty<T>(List<T> list)
    {
        return !IsNullOrEmpty(list);
    }

    private async Task ProcessOtherChainAsync(string otherChainId, string symbol, string address,
        string officialWebsite,
        List<ChainAccessInfo> chainList, List<ChainAccessInfo> otherChainList, List<string> chainIds,
        AddChainResultDto result)
    {
        // Step 1: Check if the OtherChainId is in the "Issued" state
        var chain = otherChainList.FirstOrDefault(t => t.ChainId == otherChainId);
        if (chain?.Status != TokenApplyOrderStatus.Issued.ToString())
        {
            return;
        }

        // Step 2: Prevent duplicate orders for the same OtherChainId
        var orderId = GuidHelper.UniqGuid(symbol, address, otherChainId);
        var order = await _tokenApplyOrderRepository.FindAsync(orderId);
        if (order != null)
        {
            return;
        }

        // Step 3: Create a new token apply order with "PoolInitializing" status
        chain.Status = TokenApplyOrderStatus.PoolInitializing.ToString();
        var tokenApplyOrder =
            CreateTokenApplyOrder(orderId, symbol, address, TokenApplyOrderStatus.PoolInitializing.ToString());
        tokenApplyOrder.ChainTokenInfo.Add(await CreateChainTokenInfo(chain, orderId));
        // Step 4: If ChainIds are provided, link them to the OtherChainId order
        if (IsNonEmpty(chainIds))
        {
            foreach (var accessChain in chainList)
            {
                if (chainIds.Contains(accessChain.ChainId) &&
                    accessChain.Status == TokenApplyOrderStatus.Issued.ToString())
                {
                    // Update the status of the ChainId to "PoolInitializing"
                    accessChain.Status = TokenApplyOrderStatus.PoolInitializing.ToString();
                    // Map ChainAccessInfo to ChainTokenInfo and link to the order
                    tokenApplyOrder.ChainTokenInfo.Add(await CreateChainTokenInfo(accessChain, orderId));
                }
            }
        }

        // Step 5: Insert to mysql and send lark notify
        await _tokenApplyOrderRepository.InsertAsync(tokenApplyOrder, autoSave: true);
        result.OtherChainList.Add(new AddChainDto
        {
            Id = orderId.ToString(),
            ChainId = otherChainId
        });
        await SendLarkNotifyAsync(new TokenAccessNotifyDto
        {
            Token = tokenApplyOrder.Symbol,
            Chain = otherChainId,
            TokenContract = chain.ContractAddress,
            Website = officialWebsite
        });
    }

    private async Task ProcessChainAsync(string chainId, string symbol, string address,
        string officialWebsite,
        List<ChainAccessInfo> chainList, List<string> otherChainIds, AddChainResultDto result)
    {
        // Step 1: Ensure the ChainId is in the "Issued" state and not already linked to another chain
        var chain = chainList.FirstOrDefault(t => t.ChainId == chainId);
        if (chain?.Status != TokenApplyOrderStatus.Issued.ToString() || IsNonEmpty(otherChainIds))
        {
            return;
        }

        // Step 2: Prevent duplicate orders for the same OtherChainId
        var orderId = GuidHelper.UniqGuid(symbol, address, chainId);
        var applyOrder = await _tokenApplyOrderRepository.FindAsync(orderId);
        if (applyOrder != null)
        {
            return;
        }

        // Step 3: Create a new token apply order with "PoolInitialized" status
        // Notice : When the input only includes the chain ID of the AElf chain, it indicates that other chains of AElf have already established bridge relationships with third-party chains, allowing liquidity to be added directly.
        chain.Status = TokenApplyOrderStatus.PoolInitialized.ToString();
        var tokenApplyOrder =
            CreateTokenApplyOrder(orderId, symbol, address, TokenApplyOrderStatus.PoolInitialized.ToString());
        tokenApplyOrder.ChainTokenInfo.Add(await CreateChainTokenInfo(chain, orderId));

        // Step 4: Insert to mysql and send lark notify
        await _tokenApplyOrderRepository.InsertAsync(tokenApplyOrder, autoSave: true);
        result.ChainList ??= new List<AddChainDto>();
        result.ChainList.Add(new AddChainDto
        {
            Id = orderId.ToString(),
            ChainId = chainId
        });
        await SendLarkNotifyAsync(new TokenAccessNotifyDto
        {
            Token = tokenApplyOrder.Symbol,
            Chain = chainId,
            TokenContract = chain.ContractAddress,
            Website = officialWebsite
        });
    }


    // Utility: Creates a new TokenApplyOrder
    private TokenApplyOrder CreateTokenApplyOrder(Guid orderId, string symbol, string userAddress, string status)
    {
        return new TokenApplyOrder
        {
            Id = orderId,
            Symbol = symbol,
            UserAddress = userAddress,
            Status = status,
            CreateTime = ToUtcMilliSeconds(DateTime.UtcNow),
            UpdateTime = ToUtcMilliSeconds(DateTime.UtcNow),
            StatusChangedRecords = new List<StatusChangedRecord>
            {
                new() { Id = orderId, Status = status, Time = DateTime.UtcNow }
            }
        };
    }

    // Utility: Maps ChainAccessInfo to ChainTokenInfo
    private async Task<ChainTokenInfo> CreateChainTokenInfo(ChainAccessInfo chain, Guid orderId)
    {
        var chainTokenInfo = ObjectMapper.Map<ChainAccessInfo, ChainTokenInfo>(chain);
        chainTokenInfo.Id = orderId;
        chainTokenInfo.Type = (await _chainAppService.GetAsync(chain.ChainId)).Type;
        return chainTokenInfo;
    }

    private async Task SendLarkNotifyAsync(TokenAccessNotifyDto dto)
    {
        await _larkRobotNotifyProvider.SendMessageAsync(new NotifyRequest
        {
            Template = TokenListingAlarm,
            Params = new Dictionary<string, string>
            {
                [TokenListingKeys.Token] = dto.Token,
                [TokenListingKeys.TokenContract] = dto.TokenContract,
                [TokenListingKeys.Chain] = dto.Chain,
                [TokenListingKeys.Website] = dto.Website
            }
        });
    }

    #endregion


    public async Task<UserTokenBindingDto> PrepareBindingIssueAsync(PrepareBindIssueInput input)
    {
        AssertHelper.IsTrue(!input.ChainId.IsNullOrEmpty() || !input.OtherChainId.IsNullOrEmpty(),
            "Param invalid.");
        var chainStatus = await CheckChainAccessStatusAsync(new CheckChainAccessStatusInput { Symbol = input.Symbol });
        AssertHelper.IsTrue(input.ChainId.IsNullOrEmpty() || chainStatus.ChainList.Exists(
            c => c.ChainId == input.ChainId), "Param invalid.");
        AssertHelper.IsTrue(input.OtherChainId.IsNullOrEmpty() || chainStatus.OtherChainList.Exists(
            c => c.ChainId == input.OtherChainId), "Param invalid.");

        // var address = await GetUserAddressAsync();
        var address = " ";
        var dto = new UserTokenIssueDto
        {
            Id = GuidHelper.UniqGuid(input.Symbol, address, input.OtherChainId),
            Address = address,
            WalletAddress = input.Address,
            Symbol = input.Symbol,
            ChainId = input.ChainId,
            TokenName = chainStatus.OtherChainList.FirstOrDefault(t => t.ChainId == input.OtherChainId)?.TokenName ??
                        chainStatus.ChainList.FirstOrDefault(t => t.ChainId == input.ChainId)?.TokenName,
            TokenImage = chainStatus.OtherChainList.FirstOrDefault(t => t.ChainId == input.OtherChainId)?.Icon ??
                         chainStatus.ChainList.FirstOrDefault(t => t.ChainId == input.ChainId)?.Icon,
            OtherChainId = input.OtherChainId,
            ContractAddress = input.ContractAddress,
            TotalSupply = input.Supply
        };
        return await _tokenInvokeProvider.PrepareBindingAsync(dto);
    }

    public async Task<bool> GetBindingIssueAsync(UserTokenBindingDto input)
    {
        // var address = await GetUserAddressAsync();
        var address = " ";
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");
        return await _tokenInvokeProvider.BindingAsync(input);
    }

    public async Task<PagedResultDto<TokenApplyOrderResultDto>> GetTokenApplyOrderListAsync(
        GetTokenApplyOrderListInput input)
    {
        // var address = await GetUserAddressAsync();
        var address = " ";
        if (address.IsNullOrEmpty()) return new PagedResultDto<TokenApplyOrderResultDto>();
        var mustQuery = new List<Func<QueryContainerDescriptor<TokenApplyOrderIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserAddress).Value(address)));
        QueryContainer Filter(QueryContainerDescriptor<TokenApplyOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (count, result) = await _tokenApplyOrderIndexRepository.GetListAsync(Filter, sortExp: o => o.UpdateTime,
            sortType: SortOrder.Descending, skip: input.SkipCount, limit: input.MaxResultCount);
        return new PagedResultDto<TokenApplyOrderResultDto>
        {
            Items = await LoopCollectionItemsAsync(
                ObjectMapper.Map<List<TokenApplyOrderIndex>, List<TokenApplyOrderResultDto>>(result)),
            TotalCount = count
        };
    }

    public async Task<List<TokenApplyOrderResultDto>> GetTokenApplyOrderDetailAsync(GetTokenApplyOrderInput input)
    {
        // var address = await GetUserAddressAsync();
        var address = " ";
        if (address.IsNullOrEmpty()) return new List<TokenApplyOrderResultDto>();
        var list = await GetTokenApplyOrderIndexListAsync(address, input.Symbol, input.Id, input.ChainId);
        return await LoopCollectionItemsAsync(
            ObjectMapper.Map<List<TokenApplyOrderIndex>, List<TokenApplyOrderResultDto>>(list));
    }

    private async Task<List<TokenApplyOrderResultDto>> LoopCollectionItemsAsync(List<TokenApplyOrderResultDto> itemList)
    {
        foreach (var item in itemList)
        {
            if (item.OtherChainTokenInfoResult != null &&
                (item.OtherChainTokenInfoResult.Status == TokenApplyOrderStatus.PoolInitialized.ToString() ||
                 item.OtherChainTokenInfoResult.Status == TokenApplyOrderStatus.LiquidityAdded.ToString() ||
                 item.OtherChainTokenInfoResult.Status == TokenApplyOrderStatus.Complete.ToString()))
            {
                var chainId = item.OtherChainTokenInfoResult.ChainId;
                var token = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    ChainId = chainId,
                    Address = item.OtherChainTokenInfoResult.ContractAddress
                });
                var dailyLimit =
                    await _bridgeContractAppService.GetDailyLimitAsync(
                        chainId, token.Id, CrossChainServerConsts.AElfMainChainId);
                item.OtherChainTokenInfoResult.DailyLimit = dailyLimit.DefaultDailyLimit;
                var targetChainIds = new List<string> { CrossChainServerConsts.AElfMainChainId };
                var rateLimit =
                    (await _bridgeContractAppService.GetCurrentReceiptTokenBucketStatesAsync(
                        chainId, new List<Guid> { token.Id }, targetChainIds)).FirstOrDefault();
                item.OtherChainTokenInfoResult.RateLimit = rateLimit?.RefillRate ?? 0;
                item.OtherChainTokenInfoResult.MinAmount =
                    _tokenAccessOptions.DefaultConfig.MinLiquidityInUsd.ToString();
                if (item.ChainTokenInfo != null && item.ChainTokenInfo.Count > 0)
                {
                    foreach (var chainTokenInfo in item.ChainTokenInfo)
                    {
                        if (chainTokenInfo.Status == TokenApplyOrderStatus.PoolInitialized.ToString() ||
                            chainTokenInfo.Status ==
                            TokenApplyOrderStatus.LiquidityAdded.ToString() ||
                            chainTokenInfo.Status == TokenApplyOrderStatus.Complete.ToString())
                        {
                            var aelfDailyLimit =
                                (await _indexerCrossChainLimitInfoService.GetCrossChainLimitInfoIndexAsync(
                                    chainTokenInfo.ChainId, chainId, chainTokenInfo.Symbol)).FirstOrDefault();
                            if (aelfDailyLimit != null)
                            {
                                chainTokenInfo.DailyLimit = aelfDailyLimit.DefaultDailyLimit;
                                chainTokenInfo.RateLimit = aelfDailyLimit.RefillRate;
                                chainTokenInfo.MinAmount =
                                    _tokenAccessOptions.DefaultConfig.MinLiquidityInUsd.ToString();
                            }
                        }
                    }
                }
            }
        }

        return itemList;
    }

    private async Task<long> GetTokenApplyOrderIndexListCountAsync(string address, string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TokenApplyOrderIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserAddress).Value(address)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol)));
        QueryContainer Filter(QueryContainerDescriptor<TokenApplyOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _tokenApplyOrderIndexRepository.CountAsync(Filter);
        return result.Count;
    }

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
        foreach (var chainTokenInfo in input.ChainTokenInfo)
        {
            if (chainTokenInfo.Type == BlockchainType.AElf)
            {
                index.ChainTokenInfo.Add(ObjectMapper.Map<ChainTokenInfoDto, ChainTokenInfoIndex>(chainTokenInfo));
            }
            else
            {
                index.OtherChainTokenInfo = ObjectMapper.Map<ChainTokenInfoDto, ChainTokenInfoIndex>(chainTokenInfo);
            }
        }

        foreach (var statusChangedRecord in input.StatusChangedRecords)
        {
            index.StatusChangedRecord.Add(statusChangedRecord.Status,
                ToUtcMilliSeconds(statusChangedRecord.Time).ToString());
        }

        await _tokenApplyOrderIndexRepository.AddAsync(index);
    }

    public async Task UpdateTokenApplyOrderIndexAsync(UpdateTokenApplyOrderIndexInput input)
    {
        var index = ObjectMapper.Map<UpdateTokenApplyOrderIndexInput, TokenApplyOrderIndex>(input);
        foreach (var chainTokenInfo in input.ChainTokenInfo)
        {
            if (chainTokenInfo.Type == BlockchainType.AElf)
            {
                index.ChainTokenInfo.Add(ObjectMapper.Map<ChainTokenInfoDto, ChainTokenInfoIndex>(chainTokenInfo));
            }
            else
            {
                index.OtherChainTokenInfo = ObjectMapper.Map<ChainTokenInfoDto, ChainTokenInfoIndex>(chainTokenInfo);
            }
        }

        foreach (var statusChangedRecord in input.StatusChangedRecords)
        {
            index.StatusChangedRecord.Add(statusChangedRecord.Status,
                ToUtcMilliSeconds(statusChangedRecord.Time).ToString());
        }

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

    public async Task<TokenPriceDto> GetTokenPriceAsync(GetTokenPriceInput input)
    {
        var tokenCoinId = _tokenPriceIdMappingOptions.CoinIdMapping[input.Symbol];
        if (tokenCoinId.IsNullOrEmpty())
        {
            Log.Error("Token coin id not found. {symbol}", input.Symbol);
            return new TokenPriceDto
            {
                Symbol = input.Symbol,
                TokenAmountInUsd = 0
            };
        }

        var priceInUsd = await _tokenPriceProvider.GetPriceAsync(tokenCoinId);
        var amountInUsd = input.Amount * priceInUsd;
        return new TokenPriceDto
        {
            Symbol = input.Symbol,
            TokenAmountInUsd = amountInUsd
        };
    }

    public async Task<CommitAddLiquidityDto> CommitAddLiquidityAsync(CommitAddLiquidityInput input)
    {
        // 1. check token apply order status - pool initialized
        // 2. update chain access status - liquidity added
        var order = await _tokenApplyOrderRepository.GetAsync(Guid.Parse(input.OrderId));
        if (order == null)
        {
            throw new UserFriendlyException("Order not found.");
        }

        var chainOrder = order.ChainTokenInfo.FirstOrDefault(c => c.ChainId == input.ChainId);
        if (chainOrder == null)
        {
            throw new UserFriendlyException("Chain not found.");
        }

        chainOrder.Status = TokenApplyOrderStatus.LiquidityAdded.ToString();
        await _tokenApplyOrderRepository.UpdateAsync(order);
        return new CommitAddLiquidityDto
        {
            OrderId = input.OrderId,
            ChainId = input.ChainId,
            Success = true
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
        var tokenOwnerDto = TokenOwnerList.FirstOrDefault(t => t.Symbol == symbol);
        var liquidityInUsd = !_tokenAccessOptions.TokenConfig.ContainsKey(symbol)
            ? _tokenAccessOptions.DefaultConfig.Liquidity
            : _tokenAccessOptions.TokenConfig[symbol].Liquidity;
        var holders = !_tokenAccessOptions.TokenConfig.ContainsKey(symbol)
            ? _tokenAccessOptions.DefaultConfig.Holders
            : _tokenAccessOptions.TokenConfig[symbol].Holders;
        return tokenOwnerDto.LiquidityInUsd.SafeToDecimal() > liquidityInUsd.SafeToDecimal()
               && tokenOwnerDto.Holders > holders;
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

    public static long ToUtcMilliSeconds(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }

    private static class TokenListingKeys
    {
        public const string Token = "token";
        public const string TokenContract = "tokenContract";
        public const string Chain = "chain";
        public const string Website = "website";
    }
}