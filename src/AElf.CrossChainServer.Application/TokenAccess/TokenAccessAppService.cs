using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Notify;
using AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;
using AElf.CrossChainServer.TokenAccess.UserTokenAccess;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;
using JetBrains.Annotations;
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
    private readonly IThirdUserTokenIssueRepository _thirdUserTokenIssueRepository;
    private readonly ICrossChainUserRepository _crossChainUserRepository;
    private readonly INESTRepository<TokenApplyOrderIndex, Guid> _tokenApplyOrderIndexRepository;
    private readonly INESTRepository<UserTokenAccessInfoIndex, Guid> _userAccessTokenInfoIndexRepository;
    private readonly INESTRepository<ThirdUserTokenIssueIndex, Guid> _thirdUserTokenIssueIndexRepository;

    private readonly IPoolLiquidityInfoAppService _poolLiquidityInfoAppService;
    private readonly IChainAppService _chainAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly IBridgeContractAppService _bridgeContractAppService;

    private readonly ILarkRobotNotifyProvider _larkRobotNotifyProvider;
    private readonly IAggregatePriceProvider _aggregatePriceProvider;
    private readonly ITokenInvokeProvider _tokenInvokeProvider;
    private readonly IScanProvider _scanProvider;
    private readonly IAwakenProvider _awakenProvider;
    private readonly ITokenInfoCacheProvider _tokenInfoCacheProvider;

    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly TokenWhitelistOptions _tokenWhitelistOptions;
    private readonly ChainIdMapOptions _chainIdMapOptions;

    private const int MaxMaxResultCount = 1000;
    private const int DefaultSkipCount = 0;
    private const int PageSize = 50;
    private const int MaxResultCount = 200;
    private const string TokenListingAlarm = "TokenListingAlarm";

    public TokenAccessAppService(
        ITokenApplyOrderRepository tokenApplyOrderRepository,
        INESTRepository<TokenApplyOrderIndex, Guid> tokenApplyOrderIndexRepository,
        IUserAccessTokenInfoRepository userAccessTokenInfoRepository,
        IThirdUserTokenIssueRepository thirdUserTokenIssueRepository,
        INESTRepository<UserTokenAccessInfoIndex, Guid> userAccessTokenInfoIndexRepository,
        ILarkRobotNotifyProvider larkRobotNotifyProvider, IPoolLiquidityInfoAppService poolLiquidityInfoAppService,
        IChainAppService chainAppService, ITokenAppService tokenAppService,
        ICrossChainUserRepository crossChainUserRepository, IBridgeContractAppService bridgeContractAppService,
        ITokenInvokeProvider tokenInvokeProvider,
        IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions,
        IOptionsSnapshot<TokenWhitelistOptions> tokenWhitelistOptions,
        INESTRepository<ThirdUserTokenIssueIndex, Guid> thirdUserTokenIssueIndexRepository,
        IAggregatePriceProvider aggregatePriceProvider,
        IOptionsSnapshot<ChainIdMapOptions> chainIdMapOptions, 
        IScanProvider scanProvider, IAwakenProvider awakenProvider, ITokenInfoCacheProvider tokenInfoCacheProvider)
    {
        _tokenApplyOrderRepository = tokenApplyOrderRepository;
        _tokenApplyOrderIndexRepository = tokenApplyOrderIndexRepository;
        _userAccessTokenInfoRepository = userAccessTokenInfoRepository;
        _thirdUserTokenIssueRepository = thirdUserTokenIssueRepository;
        _userAccessTokenInfoIndexRepository = userAccessTokenInfoIndexRepository;
        _larkRobotNotifyProvider = larkRobotNotifyProvider;
        _poolLiquidityInfoAppService = poolLiquidityInfoAppService;
        _chainAppService = chainAppService;
        _tokenAppService = tokenAppService;
        _crossChainUserRepository = crossChainUserRepository;
        _bridgeContractAppService = bridgeContractAppService;
        _tokenInvokeProvider = tokenInvokeProvider;
        _thirdUserTokenIssueIndexRepository = thirdUserTokenIssueIndexRepository;
        _aggregatePriceProvider = aggregatePriceProvider;
        _scanProvider = scanProvider;
        _awakenProvider = awakenProvider;
        _tokenInfoCacheProvider = tokenInfoCacheProvider;
        _tokenAccessOptions = tokenAccessOptions.Value;
        _tokenWhitelistOptions = tokenWhitelistOptions.Value;
        _chainIdMapOptions = chainIdMapOptions.Value;
    }

    public async Task<AvailableTokensDto> GetAvailableTokensAsync(GetAvailableTokensInput input)
    {
        var result = new AvailableTokensDto();
        var address = await GetUserAddressAsync();
        if (address.IsNullOrEmpty())
        {
            return result;
        }

        //step 1 : get user token holder list from scan indexer;
        var tokenHoldingList = new List<UserTokenInfoDto>();
        var skipCount = 0;
        do
        {
            var tokenList =
                await _scanProvider.GetTokenHolderListAsync(address, skipCount, PageSize, input.Symbol ?? "");
            if (tokenList == null || tokenList.TotalCount <= 0)
            {
                break;
            }

            skipCount += tokenList.Items.Count;
            //step 2 : filter already support token in bridge from config;
            var toAddTokenList =
                ObjectMapper.Map<List<IndexerTokenHolderInfoDto>, List<UserTokenInfoDto>>(tokenList.Items);
            var filteredTokens =
                toAddTokenList.Where(t => !_tokenAccessOptions.TokensToFilter.Contains(t.Symbol)).ToList();
            tokenHoldingList.AddRange(filteredTokens);
        } while (tokenHoldingList.Count == PageSize);

        tokenHoldingList = tokenHoldingList.DistinctBy(o => o.Symbol).ToList();

        //step 3 : get token info (ex. icon) from scan interface;
        foreach (var token in tokenHoldingList)
        {
            var tokenInfo = await _scanProvider.GetTokenDetailAsync(token.Symbol);
            token.TokenImage = tokenInfo?.Token.ImageUrl;
            token.TokenName = tokenInfo?.Token.Name;
            token.Holders = tokenInfo?.MergeHolders ?? 0;
            token.LiquidityInUsd = await _awakenProvider.GetTokenLiquidityInUsdAsync(token.Symbol);
            token.TotalSupply = tokenInfo?.TotalSupply ?? 0;
            token.ChainId = tokenInfo?.ChainIds.FirstOrDefault();
        }

        //step 4 : deal status, get apply order to select status; Available,Listed,Integrating.
        var supportChainList = _tokenAccessOptions.ChainWhitelistForTestnet.Count == 0
            ? _tokenAccessOptions.OtherChainIdList
            : _tokenAccessOptions.ChainWhitelistForTestnet;
        foreach (var token in tokenHoldingList)
        {
            var orderList = await GetTokenApplyOrderIndexListAsync(null, token.Symbol);
            if (orderList == null || orderList.Count == 0)
            {
                token.Status = TokenStatus.Available.ToString();
                continue;
            }

            var orderChainStatus = orderList
                .ToDictionary(order => order.ChainTokenInfo.ChainId, order => order.ChainTokenInfo.Status);

            var isAllChainHasOrder = supportChainList.All(c => orderChainStatus.ContainsKey(c));
            if (!isAllChainHasOrder)
            {
                token.Status = TokenStatus.Available.ToString();
                continue;
            }

            var isAllChainOrderComplete = supportChainList.All(c =>
                orderChainStatus.TryGetValue(c, out var status) && status == TokenApplyOrderStatus.Complete.ToString());
            token.Status = isAllChainOrderComplete ? TokenStatus.Listed.ToString() : TokenStatus.Integrating.ToString();
        }

        var tokenResultList =
            tokenHoldingList.Take(MaxResultCount).OrderBy(t => t.Status).ThenBy(t => t.Symbol).ToList();
        await _tokenInfoCacheProvider.AddTokenListAsync(tokenResultList);
        result.TokenList = ObjectMapper.Map<List<UserTokenInfoDto>, List<AvailableTokenDto>>(tokenResultList);
        return result;
    }

    private async Task<(string, long)> GetTokenLiquidityInUsdAndHoldersAsync(string symbol)
    {
        var tokenInfo = await _tokenInfoCacheProvider.GetTokenAsync(symbol);
        var holders = tokenInfo?.Holders ?? 0;
        var liquidityInUsd = tokenInfo?.LiquidityInUsd ?? "0";
        return (liquidityInUsd, holders);
    }

    public async Task<bool> CommitTokenAccessInfoAsync(UserTokenAccessInfoInput input)
    {
        var address = await GetUserAddressAsync();
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");
        AssertHelper.IsTrue(input.Email.Contains(CrossChainServerConsts.At), "Please enter a valid email address");
        AssertHelper.IsTrue(await CheckLiquidityAndHolderAvailableAsync(input.Symbol),
            "Not enough liquidity or holders");

        var dto = ObjectMapper.Map<UserTokenAccessInfoInput, UserTokenAccessInfo>(input);
        dto.Address = address;
        var existAccessInfo =
            await _userAccessTokenInfoRepository.FindAsync(o => o.Address == address && o.Symbol == input.Symbol);
        if (existAccessInfo == null)
        {
            Log.Debug("{user} create new token access info. {symbol}", address, input.Symbol);
            await _userAccessTokenInfoRepository.InsertAsync(dto);
        }
        else
        {
            Log.Debug("{user} has already applied for {symbol} token access, update record.", address, input.Symbol);
            existAccessInfo.Email = dto.Email;
            existAccessInfo.OfficialWebsite = dto.OfficialWebsite;
            existAccessInfo.PersonName = dto.PersonName;
            existAccessInfo.Title = dto.Title;
            existAccessInfo.TelegramHandler = dto.TelegramHandler;
            existAccessInfo.OfficialTwitter = dto.OfficialTwitter;
            await _userAccessTokenInfoRepository.UpdateAsync(existAccessInfo);
        }

        return true;
    }

    public async Task<UserTokenAccessInfoDto> GetUserTokenAccessInfoAsync(UserTokenAccessInfoBaseInput input)
    {
        var address = await GetUserAddressAsync();
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");
        AssertHelper.IsTrue(await CheckLiquidityAndHolderAvailableAsync(input.Symbol),
            "Not enough liquidity or holders.");
        var info = await GetUserTokenAccessInfoAsync(address, input.Symbol);
        return info.FirstOrDefault();
    }

    #region CheckChainAccessStatus

    public async Task<CheckChainAccessStatusResultDto> CheckChainAccessStatusAsync(CheckChainAccessStatusInput input)
    {
        var result = new CheckChainAccessStatusResultDto();
        var address = await GetUserAddressAsync();
        // Validate user address
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");
        AssertHelper.IsTrue(await CheckLiquidityAndHolderAvailableAsync(input.Symbol),
            "Not enough liquidity or holders.");
        // Populate chain and other chain lists
        PopulateChainLists(result, input.Symbol);

        // Update third-party token data
        await _tokenInvokeProvider.GetThirdTokenListAndUpdateAsync(address, input.Symbol);

        // Retrieve token apply order data
        var applyOrderList = await GetTokenApplyOrderIndexListAsync(address, input.Symbol);

        // Process other chain list
        await ProcessOtherChainListAsync(result.ChainList, applyOrderList, input.Symbol, address);

        return result;
    }

    // Populates chain and other chain lists
    private void PopulateChainLists(CheckChainAccessStatusResultDto result, string symbol)
    {
        result.ChainList.AddRange(_tokenAccessOptions.OtherChainIdList.Select(c => new ChainAccessInfo
        {
            ChainId = c,
            ChainName = c,
            Symbol = symbol
        }));
    }

    // Processes the chain list to determine the status of each chain
    private async Task ProcessOtherChainListAsync(
        List<ChainAccessInfo> otherChainList,
        List<TokenApplyOrderIndex> applyOrderList,
        string symbol,
        string address)
    {
        foreach (var item in otherChainList)
        {
            // Check if the token is completed on the chain
            var isCompleted = _tokenWhitelistOptions.TokenWhitelist.ContainsKey(symbol) &&
                              _tokenWhitelistOptions.TokenWhitelist[symbol].ContainsKey(item.ChainId);

            // Get the status from the apply order list
            var applyOrder = applyOrderList.FirstOrDefault(t => t.ChainTokenInfo != null &&
                                                                t.ChainTokenInfo.ChainId == item.ChainId);
            var applyStatus = applyOrder?.ChainTokenInfo?.Status;
            // Retrieve user token issue details
            var res = await _thirdUserTokenIssueRepository.FindAsync(o =>
                o.Address == address && o.OtherChainId == item.ChainId && o.Symbol == item.Symbol);

            item.TotalSupply = res?.TotalSupply.SafeToDecimal() ?? 0M;
            item.Decimals = CrossChainServerConsts.DefaultEvmTokenDecimal;
            item.TokenName = res?.TokenName;
            item.ContractAddress = res?.ContractAddress;
            item.Icon = res?.TokenImage;

            // Determine the status
            item.Status = DetermineStatus(isCompleted, applyStatus, res);

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
    private static string DetermineStatus(bool isCompleted, string applyStatus, ThirdUserTokenIssueInfo res)
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

        return TokenApplyOrderStatus.Unissued.ToString();
    }

    // Determines if the token is selected
    private static bool DetermineCheckedStatus(bool isCompleted, List<TokenApplyOrderIndex> applyOrderList,
        string chainId)
    {
        return isCompleted ||
               applyOrderList.Exists(t =>
                   t.ChainTokenInfo?.ChainId == chainId);
    }

    #endregion

    #region AddChain

    public async Task<AddChainResultDto> AddChainAsync(AddChainInput input)
    {
        var address = await GetUserAddressAsync();
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");
        AssertHelper.IsTrue(await CheckLiquidityAndHolderAvailableAsync(input.Symbol),
            "Not enough liquidity or holders.");

        var userAccessInfo =
            await _userAccessTokenInfoRepository.FindAsync(t => t.Symbol == input.Symbol && t.Address == address);

        // 1. Check chain access status - Get the status of my token on all chains
        var chainAccessStatus = await CheckChainAccessStatusAsync(new CheckChainAccessStatusInput
        {
            Symbol = input.Symbol
        });

        // Validate provided chain IDs against available chain access status
        ValidateChainIds(input.ChainIds, chainAccessStatus.ChainList, "Failed to add chain.");

        var result = new AddChainResultDto();
        // 2. Process other chain
        if (IsNonEmpty(input.ChainIds))
        {
            foreach (var otherChainId in input.ChainIds)
            {
                await ProcessOtherChainAsync(otherChainId, input.Symbol, address, userAccessInfo?.OfficialWebsite,
                    chainAccessStatus.ChainList, result);
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
        string officialWebsite, List<ChainAccessInfo> otherChainList,
        AddChainResultDto result)
    {
        // Step 1: Check if the OtherChainId is in the "Issued" state
        var chain = otherChainList.FirstOrDefault(t => t.ChainId == otherChainId);
        if (chain?.Status != TokenApplyOrderStatus.Issued.ToString())
        {
            Log.Debug("Chain {chainId} is not in the 'Issued' state.", otherChainId);
            return;
        }

        // Step 2: Prevent duplicate orders for the same OtherChainId
        var order = await _tokenApplyOrderRepository.FindAsync(o =>
            o.Symbol == symbol && o.ChainId == otherChainId);
        if (order != null)
        {
            Log.Debug("Order {token},{chainId} already exists.", symbol, otherChainId);
            return;
        }

        if (await CheckIfLiquidityHasBeenAddedAsync(chain.ContractAddress, otherChainId, symbol, chain.TotalSupply))
        {
            Log.Debug("Liquidity has been added for {token} on {chainId}.", symbol, otherChainId);
            chain.Status = TokenApplyOrderStatus.PoolInitialized.ToString();
        }
        else
        {
            // Step 3: Create a new token apply order with "PoolInitializing" status
            chain.Status = TokenApplyOrderStatus.PoolInitializing.ToString();
        }
        var tokenApplyOrder =
            CreateTokenApplyOrder(symbol, address, chain.Status, chain);

        // Step 5: Insert to mysql and send lark notify
        var orderInsert = await _tokenApplyOrderRepository.InsertAsync(tokenApplyOrder);
        result.ChainList ??= new List<AddChainDto>();
        result.ChainList.Add(new AddChainDto
        {
            Id = orderInsert.Id.ToString(),
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

    private async Task<bool> CheckIfLiquidityHasBeenAddedAsync(string tokenAddress, string chainId, string symbol,
        decimal totalSupply)
    {
        Log.Debug("Check if liquidity has been added for {tokenAddress},{token} on {chainId}.", tokenAddress, symbol,
            chainId);
        totalSupply = totalSupply == 0
            ? (await _tokenInfoCacheProvider.GetTokenAsync(symbol)).TotalSupply
            : totalSupply;
        Log.Debug("Total supply of {token} is {totalSupply}.", tokenAddress, totalSupply);
        var liquidity = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(new GetPoolLiquidityInfosInput
        {
            Token = tokenAddress,
            ChainId = chainId
        });
        if (liquidity.TotalCount > 0)
        {
            var liq = liquidity.Items.FirstOrDefault()?.Liquidity;
            Log.Debug("Liquidity has been added for {token} on {chainId}, totalSupply:{total}.", tokenAddress, chainId,
                liq);
            return liq == totalSupply;
        }

        return false;
    }

    // Utility: Creates a new TokenApplyOrder
    private TokenApplyOrder CreateTokenApplyOrder(string symbol, string userAddress, string status,
        ChainAccessInfo chain)
    {
        Log.Debug("Create token apply order for {symbol} on {chainId}.chain info:{chainInfo}", symbol, chain.ChainId,
            JsonSerializer.Serialize(chain));
        return new TokenApplyOrder
        {
            Symbol = symbol,
            UserAddress = userAddress,
            Status = status,
            CreateTime = ToUtcMilliSeconds(DateTime.UtcNow),
            UpdateTime = ToUtcMilliSeconds(DateTime.UtcNow),
            StatusChangedRecords = new List<StatusChangedRecord>
            {
                new() { Id = Guid.NewGuid(), Status = status, Time = DateTime.UtcNow }
            },
            ChainId = chain.ChainId,
            ChainName = chain.ChainName,
            TokenName = chain.TokenName,
            TotalSupply = chain.TotalSupply,
            Decimals = chain.Decimals,
            Icon = chain.Icon,
            PoolAddress = chain.PoolAddress,
            ContractAddress = chain.ContractAddress
        };
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
        AssertHelper.IsTrue(CheckAddress(input.Address), $"Invalid other chain address.{input.ChainId}");
        var chainStatus = await CheckChainAccessStatusAsync(new CheckChainAccessStatusInput { Symbol = input.Symbol });
        AssertHelper.IsTrue(input.ChainId.IsNullOrEmpty() || chainStatus.ChainList.Exists(
            c => c.ChainId == input.ChainId), $"Invalid chainId {input.ChainId}.");
        var address = await GetUserAddressAsync();
        var token = await _tokenInfoCacheProvider.GetTokenAsync(input.Symbol);
        var dto = new ThirdUserTokenIssueInfoDto
        {
            Address = address,
            WalletAddress = input.Address,
            Symbol = input.Symbol,
            ChainId = token.ChainId ?? CrossChainServerConsts.AElfMainChain,
            TokenName =
                chainStatus.ChainList.FirstOrDefault(t => t.ChainId == input.ChainId)?.TokenName ?? token.TokenName,
            TokenImage =
                chainStatus.ChainList.FirstOrDefault(t => t.ChainId == input.ChainId)?.Icon ?? token.TokenImage,
            OtherChainId = input.ChainId,
            ContractAddress = input.ContractAddress,
            TotalSupply = input.Supply
        };
        return await _tokenInvokeProvider.PrepareBindingAsync(dto);
    }

    private static bool CheckAddress(string address)
    {
        return Nethereum.Util.AddressExtensions.IsValidEthereumAddressHexFormat(address) || TonAddressHelper.IsTonFriendlyAddress(address);
    }

    public async Task<bool> GetBindingIssueAsync(UserTokenBindingDto input)
    {
        var address = await GetUserAddressAsync();
        AssertHelper.IsTrue(!address.IsNullOrEmpty(), "No permission.");
        return await _tokenInvokeProvider.BindingAsync(input);
    }

    public async Task<PagedResultDto<TokenApplyOrderDto>> GetTokenApplyOrderListAsync(
        GetTokenApplyOrderListInput input)
    {
        var address = await GetUserAddressAsync();
        if (address.IsNullOrEmpty()) return new PagedResultDto<TokenApplyOrderDto>();
        var mustQuery = new List<Func<QueryContainerDescriptor<TokenApplyOrderIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserAddress).Value(address)));
        QueryContainer Filter(QueryContainerDescriptor<TokenApplyOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (count, result) = await _tokenApplyOrderIndexRepository.GetListAsync(Filter, sortExp: o => o.UpdateTime,
            sortType: SortOrder.Descending, skip: input.SkipCount, limit: input.MaxResultCount);
        return new PagedResultDto<TokenApplyOrderDto>
        {
            Items = await LoopCollectionItemsAsync(
                ObjectMapper.Map<List<TokenApplyOrderIndex>, List<TokenApplyOrderDto>>(result)),
            TotalCount = count
        };
    }

    public async Task<List<TokenApplyOrderDto>> GetTokenApplyOrderDetailAsync(GetTokenApplyOrderInput input)
    {
        var address = await GetUserAddressAsync();
        if (address.IsNullOrEmpty()) return new List<TokenApplyOrderDto>();
        var list = await GetTokenApplyOrderIndexListAsync(address, input.Symbol, input.Id, input.ChainId);
        return await LoopCollectionItemsAsync(
            ObjectMapper.Map<List<TokenApplyOrderIndex>, List<TokenApplyOrderDto>>(list));
    }

    private async Task<List<TokenApplyOrderDto>> LoopCollectionItemsAsync(List<TokenApplyOrderDto> itemList)
    {
        foreach (var item in itemList)
        {
            if (item.ChainTokenInfo != null && IsStatusValid(item.ChainTokenInfo.Status))
            {
                await ProcessOtherChainTokenInfoAsync(item.ChainTokenInfo);
            }
        }

        return itemList;
    }

    private static bool IsStatusValid(string status)
    {
        return status == TokenApplyOrderStatus.Complete.ToString();
    }

    private async Task ProcessOtherChainTokenInfoAsync(ChainTokenInfoResultDto otherChainTokenInfo)
    {
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = otherChainTokenInfo.ChainId,
            Address = otherChainTokenInfo.ContractAddress
        });

        var dailyLimit = await _bridgeContractAppService.GetDailyLimitAsync(
            otherChainTokenInfo.ChainId, token.Id, CrossChainServerConsts.AElfMainChainId);

        otherChainTokenInfo.DailyLimit = dailyLimit.DefaultDailyLimit;

        var rateLimit = (await _bridgeContractAppService.GetCurrentReceiptTokenBucketStatesAsync(
                otherChainTokenInfo.ChainId, new List<Guid> { token.Id },
                new List<string> { CrossChainServerConsts.AElfMainChainId }))
            .FirstOrDefault();

        otherChainTokenInfo.RateLimit = rateLimit?.RefillRate ?? 0;
        otherChainTokenInfo.MinAmount = _tokenAccessOptions.DefaultConfig.MinLiquidityInUsd.ToString();
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

    public async Task AddThirdUserTokenIssueInfoIndexAsync(AddThirdUserTokenIssueInfoIndexInput input)
    {
        var index = ObjectMapper.Map<AddThirdUserTokenIssueInfoIndexInput, ThirdUserTokenIssueIndex>(input);
        await _thirdUserTokenIssueIndexRepository.AddAsync(index);
    }

    public async Task UpdateThirdUserTokenIssueInfoIndexAsync(UpdateThirdUserTokenIssueInfoIndexInput input)
    {
        var index = ObjectMapper.Map<UpdateThirdUserTokenIssueInfoIndexInput, ThirdUserTokenIssueIndex>(input);
        await _thirdUserTokenIssueIndexRepository.UpdateAsync(index);
    }

    public async Task AddTokenApplyOrderIndexAsync(AddTokenApplyOrderIndexInput input)
    {
        var index = ObjectMapper.Map<AddTokenApplyOrderIndexInput, TokenApplyOrderIndex>(input);
        index.ChainTokenInfo = new ChainTokenInfoIndex()
        {
            Symbol = input.Symbol,
            ChainId = input.ChainId,
            ChainName = input.ChainName,
            TokenName = input.TokenName,
            TotalSupply = input.TotalSupply,
            Decimals = input.Decimals,
            Icon = input.Icon,
            PoolAddress = input.PoolAddress,
            ContractAddress = input.ContractAddress,
            Status = input.Status
        };
        foreach (var statusChangedRecord in input.StatusChangedRecords)
        {
            index.StatusChangedRecord ??= new Dictionary<string, string>();
            index.StatusChangedRecord.Add(statusChangedRecord.Status,
                ToUtcMilliSeconds(statusChangedRecord.Time).ToString());
        }

        Log.Debug("AddTokenApplyOrderIndexAsync start.{orderId}", input.Id);
        await _tokenApplyOrderIndexRepository.AddAsync(index);
    }

    public async Task UpdateTokenApplyOrderIndexAsync(UpdateTokenApplyOrderIndexInput input)
    {
        var index = ObjectMapper.Map<UpdateTokenApplyOrderIndexInput, TokenApplyOrderIndex>(input);
        index.ChainTokenInfo = new ChainTokenInfoIndex()
        {
            Symbol = input.Symbol,
            ChainId = input.ChainId,
            ChainName = input.ChainName,
            TokenName = input.TokenName,
            TotalSupply = input.TotalSupply,
            Decimals = input.Decimals,
            Icon = input.Icon,
            PoolAddress = input.PoolAddress,
            ContractAddress = input.ContractAddress,
            Status = input.Status
        };
        foreach (var statusChangedRecord in input.StatusChangedRecords)
        {
            index.StatusChangedRecord ??= new Dictionary<string, string>();
            index.StatusChangedRecord.Add(statusChangedRecord.Status,
                ToUtcMilliSeconds(statusChangedRecord.Time).ToString());
        }

        Log.Debug("UpdateTokenApplyOrderIndexAsync start.{orderId}", input.Id);
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

    #region TokenWhitelist

    public async Task<Dictionary<string, Dictionary<string, TokenInfoDto>>> GetTokenWhitelistAsync()
    {
        var tokenWhitelist = _tokenWhitelistOptions.TokenWhitelist;
        var result = tokenWhitelist.ToDictionary(
            tokenEntry => tokenEntry.Key,
            tokenEntry => tokenEntry.Value.ToDictionary(
                chainEntry => chainEntry.Key,
                chainEntry => ObjectMapper.Map<TokenInfo, TokenInfoDto>(chainEntry.Value)
            ));
        // step 1: get complete order chain token info;
        // step 2: get main and dapp chain liquidity;
        // step 3: set token info flags;
        // step 4: if main or dapp chain token isburnable is false, only main or dapp;
        var orderList =
            await GetTokenApplyOrderIndexListAsync(null, null, null, null, TokenApplyOrderStatus.Complete.ToString());
        var liquidityList = await _poolLiquidityInfoAppService.GetPoolLiquidityInfosAsync(
            new GetPoolLiquidityInfosInput
            {
                MaxResultCount = MaxMaxResultCount,
                SkipCount = DefaultSkipCount
            });
        var symbolChainOrderLiquidityMap = liquidityList.Items
            .GroupBy(liq => liq.TokenInfo.Symbol)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(liq => liq.ChainId, liq => liq.Liquidity)
            );

        foreach (var order in orderList)
        {
            Log.Debug("GetTokenWhitelistAsync start.{symbol}", order.Symbol);
            var chainTokenInfoMap = result.TryGetValue(order.Symbol, out var chainTokenMap)
                ? chainTokenMap
                : new Dictionary<string, TokenInfoDto>();

            var tokenInfo = InitializeEvmTokenInfo(order.Symbol, order.ChainTokenInfo);
            if (!_chainIdMapOptions.Chain.TryGetValue(order.ChainTokenInfo.ChainId, out var chainId))
            {
                continue;
            }

            if (symbolChainOrderLiquidityMap.TryGetValue(order.Symbol, out var liquidityMap))
            {
                SetEvmTokenFlags(order.ChainTokenInfo.ChainId, tokenInfo, liquidityMap);
            }

            chainTokenInfoMap.TryAdd(chainId, tokenInfo);
            foreach (var aelfChainId in _tokenAccessOptions.ChainIdList)
            {
                var token = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    ChainId = aelfChainId,
                    Symbol = order.Symbol
                });
                if (!token.IsBurnable)
                {
                    var tokenInfoAelfIssue = InitializeAelfTokenInfo(order.Symbol, token);
                    if (symbolChainOrderLiquidityMap.TryGetValue(order.Symbol, out var liquiditySideMap))
                    {
                        var chain = await _chainAppService.GetByAElfChainIdAsync(token.IssueChainId);
                        var chainIdConvert = ChainHelper.ConvertChainIdToBase58(token.IssueChainId);
                        SetAelfTokenFlags(chain.Id, tokenInfoAelfIssue, liquiditySideMap);
                        chainTokenInfoMap.TryAdd(chainIdConvert, tokenInfoAelfIssue);
                    }
                    break;
                }
                var chainIdAelfConvert = ConvertToTargetChainId(aelfChainId);
                var tokenInfoAelf = InitializeAelfTokenInfo(order.Symbol, token);
                if (symbolChainOrderLiquidityMap.TryGetValue(order.Symbol, out var liquidityMainMap))
                {
                    SetAelfTokenFlags(aelfChainId, tokenInfoAelf, liquidityMainMap);
                    chainTokenInfoMap.TryAdd(chainIdAelfConvert, tokenInfoAelf);
                }
            }
            result[order.Symbol] = chainTokenInfoMap;
        }

        return result;
    }
    
    private string ConvertToTargetChainId(string sourceChainId)
        => _tokenAccessOptions.ChainIdMap.FirstOrDefault(kvp => kvp.Value == sourceChainId).Key ?? string.Empty;

    private void SetAelfTokenFlags(string chainId, TokenInfoDto tokenInfoDto, Dictionary<string, decimal> liquidityMap)
    {
        var aelfChainLiquidity = liquidityMap.TryGetValue(chainId, out var liquidity) ? liquidity : 0;
        var hasOtherChainLiquidity = _tokenAccessOptions.OtherChainIdList
            .Any(otherChainId => liquidityMap.TryGetValue(otherChainId, out var evmLiquidity) && evmLiquidity > 0);
        tokenInfoDto.OnlyFrom = hasOtherChainLiquidity && aelfChainLiquidity <= 0;
        tokenInfoDto.OnlyTo = aelfChainLiquidity > 0 && !hasOtherChainLiquidity;
    }

    private void SetEvmTokenFlags(string chainId, TokenInfoDto tokenInfoDto, Dictionary<string, decimal> liquidityMap)
    {
        var evmLiquidity = liquidityMap.TryGetValue(chainId, out var liquidity) ? liquidity : 0;
        var hasAelfChainLiquidity = _tokenAccessOptions.ChainIdList
            .Any(aelfChainId => liquidityMap.TryGetValue(aelfChainId, out var aelfLiquidity) && aelfLiquidity > 0);
        tokenInfoDto.OnlyFrom = hasAelfChainLiquidity && evmLiquidity <= 0;
        tokenInfoDto.OnlyTo = evmLiquidity > 0 && !hasAelfChainLiquidity;
    }

    private static TokenInfoDto InitializeEvmTokenInfo(string symbol, ChainTokenInfoIndex chainToken)
    {
        return new TokenInfoDto
        {
            Symbol = symbol,
            Name = chainToken.TokenName,
            Address = chainToken.ContractAddress,
            Icon = chainToken.Icon,
            Decimals = chainToken.Decimals,
            IsNativeToken = false,
            OnlyTo = false,
            OnlyFrom = false
        };
    }

    private static TokenInfoDto InitializeAelfTokenInfo(string symbol, TokenDto token)
    {
        return new TokenInfoDto
        {
            Symbol = symbol,
            Name = token.Symbol,
            Address = token.Address,
            Icon = token.Icon,
            Decimals = token.Decimals,
            IsNativeToken = false,
            IssueChainId = token.IssueChainId.ToString(),
            OnlyTo = false,
            OnlyFrom = false
        };
    }

    #endregion

    

    public async Task<TokenPriceDto> GetTokenPriceAsync(GetTokenPriceInput input)
    {
        var result = new TokenPriceDto
        {
            Symbol = input.Symbol
        };
        var priceInUsd = await _aggregatePriceProvider.GetPriceAsync(input.Symbol);
        var amountInUsd = input.Amount * priceInUsd;
        result.TokenAmountInUsd = amountInUsd;
        return result;
    }

    public async Task<bool> TriggerOrderStatusChangeAsync(TriggerOrderStatusChangeInput input)
    {
        var queryable =
            await _tokenApplyOrderRepository.WithDetailsAsync(y => y.StatusChangedRecords);
        var query = queryable.Where(x => x.Id == Guid.Parse(input.OrderId));
        var order = await AsyncExecuter.FirstOrDefaultAsync(query);
        if (order == null || order.Status != TokenApplyOrderStatus.PoolInitialized.ToString())
        {
            throw new UserFriendlyException($"Invalid order {order.Id.ToString()}.");
        }
        
        order.ContractAddress ??= input.ChainIdTokenInfo.TokenContractAddress;
        order.Decimals = order.Decimals == 0
            ? order.Decimals
            : input.ChainIdTokenInfo.TokenDecimals;
        order.Status = TokenApplyOrderStatus.Complete.ToString();
        order.StatusChangedRecords.Add(new StatusChangedRecord
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Status = TokenApplyOrderStatus.Complete.ToString(),
            Time = DateTime.UtcNow
        });
        Log.Debug("TriggerOrderStatusChangeAsync start.{orderId}", input.OrderId);
        await _tokenApplyOrderRepository.UpdateAsync(order);
        return true;
    }

    

    private async Task<string> GetUserAddressAsync()
    {
        var userId = CurrentUser.IsAuthenticated ? CurrentUser?.GetId() : null;
        if (!userId.HasValue) return null;
        var userDto = await _crossChainUserRepository.FindAsync(o => o.UserId == userId);
        return userDto?.AddressInfos?.FirstOrDefault()?.Address;
    }

    private async Task<bool> CheckLiquidityAndHolderAvailableAsync(string symbol)
    {
        var (liquidityInUsdFromCache, holdersFromCache) = await GetTokenLiquidityInUsdAndHoldersAsync(symbol);
        var liquidityInUsd = !_tokenAccessOptions.TokenConfig.ContainsKey(symbol)
            ? _tokenAccessOptions.DefaultConfig.Liquidity
            : _tokenAccessOptions.TokenConfig[symbol].Liquidity;
        var holders = !_tokenAccessOptions.TokenConfig.ContainsKey(symbol)
            ? _tokenAccessOptions.DefaultConfig.Holders
            : _tokenAccessOptions.TokenConfig[symbol].Holders;

        var decimalEnough = liquidityInUsdFromCache.SafeToDecimal() > liquidityInUsd.SafeToDecimal();
        Log.Debug("Check Liquidity available, owner: {owner} and option: {option}",
            liquidityInUsdFromCache.SafeToDecimal(), liquidityInUsd.SafeToDecimal());
        var holdersEnough = holdersFromCache > holders;
        Log.Debug("Check Holders available, owner: {owner} and option: {option}", holdersFromCache, holders);
        return decimalEnough && holdersEnough;
    }

    private async Task<List<UserTokenAccessInfoDto>> GetUserTokenAccessInfoAsync(string address,
        [CanBeNull] string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserTokenAccessInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));
        if (symbol != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol)));
        }

        QueryContainer Filter(QueryContainerDescriptor<UserTokenAccessInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var list = await _userAccessTokenInfoIndexRepository.GetListAsync(Filter);
        return ObjectMapper.Map<List<UserTokenAccessInfoIndex>, List<UserTokenAccessInfoDto>>(list.Item2);
    }

    private async Task<List<TokenApplyOrderIndex>> GetTokenApplyOrderIndexListAsync(string address, string symbol,
        string id = null, string chainId = null, string status = null)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TokenApplyOrderIndex>, QueryContainer>>();
        if (!address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.UserAddress).Value(address)));
        }

        if (!symbol.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol)));
        }

        if (!id.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(id)));
        }

        if (!status.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Status).Value(status)));
        }

        if (!chainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Bool(i => i.Should(
                s => s.Match(k =>
                    k.Field("chainTokenInfo.chainId").Query(chainId)),
                s => s.Term(k =>
                    k.Field(f => f.ChainTokenInfo.ChainId).Value(chainId)))));
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