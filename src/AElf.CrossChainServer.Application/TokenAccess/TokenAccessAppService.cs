using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.TokenAccess;

[RemoteService(IsEnabled = false)]
public class TokenAccessAppService : CrossChainServerAppService, ITokenAccessAppService
{
    private readonly ISymbolMarketProvider _symbolMarketProvider;
    private readonly ILiquidityDataProvider _liquidityDataProvider;
    private readonly IScanProvider _scanProvider;
    private readonly ITokenApplyOrderRepository _tokenApplyOrderRepository;
    private readonly INESTRepository<TokenApplyOrderIndex, Guid> _tokenApplyOrderIndexRepository;
    private readonly IUserAccessTokenInfoRepository _userAccessTokenInfoRepository;
    private readonly INESTRepository<UserTokenAccessInfoIndex, Guid> _userAccessTokenInfoIndexRepository;
    private readonly AElfClientProvider _aElfClientProvider;
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly ILarkManager _larkManager;

    public TokenAccessAppService(ISymbolMarketProvider symbolMarketProvider, 
        ILiquidityDataProvider liquidityDataProvider, 
        IScanProvider scanProvider, ITokenApplyOrderRepository tokenApplyOrderRepository, 
        INESTRepository<TokenApplyOrderIndex, Guid> tokenApplyOrderIndexRepository,
        IUserAccessTokenInfoRepository userAccessTokenInfoRepository, 
        INESTRepository<UserTokenAccessInfoIndex, Guid> userAccessTokenInfoIndexRepository, 
        AElfClientProvider aElfClientProvider, 
        IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions, 
        ILarkManager larkManager)
    {
        _symbolMarketProvider = symbolMarketProvider;
        _liquidityDataProvider = liquidityDataProvider;
        _scanProvider = scanProvider;
        _tokenApplyOrderRepository = tokenApplyOrderRepository;
        _tokenApplyOrderIndexRepository = tokenApplyOrderIndexRepository;
        _userAccessTokenInfoRepository = userAccessTokenInfoRepository;
        _userAccessTokenInfoIndexRepository = userAccessTokenInfoIndexRepository;
        _aElfClientProvider = aElfClientProvider;
        _tokenAccessOptions = tokenAccessOptions.Value;
        _larkManager = larkManager;
    }

    public async Task<AvailableTokensDto> GetAvailableTokensAsync(string address)
    {
        var tokenList = await _scanProvider.GetOwnTokensAsync(address);
        var symbolMarketTokenList = await _symbolMarketProvider.GetOwnTokensAsync(address);
        tokenList.AddRange(symbolMarketTokenList);
        foreach (var token in tokenList)
        {
            token.LiquidityInUsd = await _liquidityDataProvider.GetTokenTvlAsync(token.Symbol);
        }

        return new AvailableTokensDto()
        {
            TokenList = tokenList
        };
    }

    public async Task CommitTokenAccessInfoAsync(UserTokenAccessInfoInput input)
    {
        var token = await _aElfClientProvider.GetTokenAsync(CrossChainServerConsts.AElfMainChainId, null, input.Symbol);
        if (token.Owner != input.Address)
        {
            throw new UserFriendlyException("No permission.");
        }

        var userTokenAccessInfo = await _userAccessTokenInfoRepository.FirstOrDefaultAsync(t => t.Symbol == input.Symbol);
        if (userTokenAccessInfo == null)
        {
            await _userAccessTokenInfoRepository.InsertAsync(ObjectMapper.Map<UserTokenAccessInfoInput, UserTokenAccessInfo>(input));
        }
        else
        {
            await _userAccessTokenInfoRepository.UpdateAsync(ObjectMapper.Map<UserTokenAccessInfoInput, UserTokenAccessInfo>(input));
        }
    }

    public async Task<UserTokenAccessInfoDto> GetUserTokenAccessInfoAsync(UserTokenAccessInfoInput input)
    {
        return ObjectMapper.Map<UserTokenAccessInfoIndex, UserTokenAccessInfoDto>(await GetUserTokenAccessInfoIndexAsync(input.Symbol));
    }

    private async Task<UserTokenAccessInfoIndex> GetUserTokenAccessInfoIndexAsync(string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserTokenAccessInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol)));
        QueryContainer Filter(QueryContainerDescriptor<UserTokenAccessInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _userAccessTokenInfoIndexRepository.GetAsync(Filter);
    }
    
    private async Task<List<TokenApplyOrderIndex>> GetTokenApplyOrderIndexListAsync(string address, string symbol = null, List<TokenApplyOrderStatus> statusList = null)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TokenApplyOrderIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserAddress).Value(address)));
        if (String.IsNullOrWhiteSpace(symbol))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol)));
        }
        if (!statusList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Status).Terms(statusList)));
        }
        QueryContainer Filter(QueryContainerDescriptor<TokenApplyOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _tokenApplyOrderIndexRepository.GetListAsync(Filter);
        return result.Item2;
    }

    public async Task<CheckChainAccessStatusResultDto> CheckChainAccessStatusAsync(string symbol, string address)
    {
        var result = new CheckChainAccessStatusResultDto();
        foreach (var chainId in _tokenAccessOptions.ChainIdList)
        {
            result.ChainList.Add(new ChainAccessInfo
            {
                ChainId = chainId,
            });
        }
        
        foreach (var otherChainId in _tokenAccessOptions.ChainIdList)
        {
            result.OtherChainList.Add(new ChainAccessInfo
            {
                ChainId = otherChainId,
            });
        }
        var userAccessTokenInfoIndex = await GetUserTokenAccessInfoIndexAsync(symbol);
        if (userAccessTokenInfoIndex != null)
        {
            var chainIds = JsonSerializer.Deserialize<List<string>>(userAccessTokenInfoIndex.ChainIds);
            var otherChainIds = JsonSerializer.Deserialize<List<string>>(userAccessTokenInfoIndex.OtherChainIds);
            foreach (var chainId in chainIds)
            {
                result.ChainList.First(t => t.ChainId == chainId).Status = ChainAccessStatus.Accessed;
            }
            foreach (var otherChainId in otherChainIds)
            {
                result.OtherChainList.First(t => t.ChainId == otherChainId).Status = ChainAccessStatus.Accessed;
            }
        }
        var applyOrderList = await GetTokenApplyOrderIndexListAsync(address, symbol, new List<TokenApplyOrderStatus> {TokenApplyOrderStatus.PoolInitializing, TokenApplyOrderStatus.AddLiquidity});
        foreach (var applyOrderIndex in applyOrderList)
        {
            var chainIds = JsonSerializer.Deserialize<List<string>>(applyOrderIndex.ChainIds);
            foreach (var chainId in chainIds)
            {
                result.ChainList.First(t => t.ChainId == chainId).Status = ChainAccessStatus.Accessing;
            }
            result.OtherChainList.First(t => t.ChainId == applyOrderIndex.OtherChainId).Status = ChainAccessStatus.Accessing;
        }

        var unissuedChainList = result.OtherChainList.Where(t => t.Status == ChainAccessStatus.Unissued).ToList();
        var issueChainList = await _symbolMarketProvider.GetIssueChainListAsync(symbol);
        foreach (var otherChainInfo in unissuedChainList)
        {
            if (issueChainList.Contains(otherChainInfo.ChainId))
            {
                otherChainInfo.Status = ChainAccessStatus.Issued;
            }
        }
        return result;
    }

    public async Task SelectChainAsync(SelectChainInput input)
    {
        foreach (var chainId in input.ChainIds)
        {
            if (!_tokenAccessOptions.ChainIdList.Contains(chainId))
            {
                throw new UserFriendlyException("invalid chainId");
            }
        }
        foreach (var otherChainId in input.OtherChainIds)
        {
            if (!_tokenAccessOptions.OtherChainIdList.Contains(otherChainId))
            {
                throw new UserFriendlyException("invalid otherChainId");
            }
        }

        var userAccessTokenInfo = await _userAccessTokenInfoRepository.GetAsync(t => t.Symbol == input.Symbol);
        if (userAccessTokenInfo.Address != input.Address)
        {
            throw new UserFriendlyException("No permission.");
        }
        var addChainIds = input.ChainIds.Except(JsonSerializer.Deserialize<List<string>>(userAccessTokenInfo.ChainIds)).ToList();
        var addOtherChainIds = input.OtherChainIds.Except(JsonSerializer.Deserialize<List<string>>(userAccessTokenInfo.OtherChainIds)).ToList();
        if (addChainIds.IsNullOrEmpty() && addOtherChainIds.IsNullOrEmpty())
        {
            return;
        }

        // split order
        var applyOrderList = new List<TokenApplyOrder>();
        var firstApplyOrder = new TokenApplyOrder
        {
            Symbol = userAccessTokenInfo.Symbol,
            UserAddress = userAccessTokenInfo.Address,
            ChainIds = JsonSerializer.Serialize(addChainIds),
        };
        if (addOtherChainIds.Count > 0)
        {
            firstApplyOrder.OtherChainId = addOtherChainIds[0];
            addOtherChainIds.RemoveAt(0);
        }
        applyOrderList.Add(firstApplyOrder);
        foreach (var otherChainId in addOtherChainIds)
        {
            applyOrderList.Add(new TokenApplyOrder()
            {
                Symbol = userAccessTokenInfo.Symbol,
                UserAddress = userAccessTokenInfo.Address,
                OtherChainId = otherChainId
            });
        }
        await _tokenApplyOrderRepository.InsertManyAsync(applyOrderList);
        await _larkManager.SendMessageAsync("");
    }

    public async Task IssueTokenAsync(IssueTokenInput input)
    {
        await _symbolMarketProvider.IssueTokenAsync(input);
    }

    public async Task<TokenApplyOrderListDto> GetTokenApplyOrderListAsync(GetTokenApplyOrderListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TokenApplyOrderIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserAddress).Value(input.Address)));
        QueryContainer Filter(QueryContainerDescriptor<TokenApplyOrderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _tokenApplyOrderIndexRepository.GetListAsync(Filter, sortExp: o => o.UpdateTime, 
            sortType: SortOrder.Descending, skip: input.SkipCount, limit: input.MaxResultCount);
        var totalCount = await _tokenApplyOrderIndexRepository.CountAsync(Filter);
        return new TokenApplyOrderListDto()
        {
            Items = ObjectMapper.Map<List<TokenApplyOrderIndex>, List<TokenApplyOrderDto>>(result.Item2),
            TotalCount = totalCount.Count
        };
    }

    public async Task<TokenApplyOrderDto> GetTokenApplyOrderAsync(Guid id)
    {
        var tokenApplyOrder = await _tokenApplyOrderIndexRepository.GetAsync(id);
        return ObjectMapper.Map<TokenApplyOrderIndex, TokenApplyOrderDto>(tokenApplyOrder);
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
}