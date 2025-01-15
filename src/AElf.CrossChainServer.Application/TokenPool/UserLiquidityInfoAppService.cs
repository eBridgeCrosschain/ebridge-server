using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.TokenAccess;
using AElf.CrossChainServer.TokenPrice;
using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;
using Volo.Abp;

namespace AElf.CrossChainServer.TokenPool;

[RemoteService(IsEnabled = false)]
public class UserLiquidityInfoAppService : CrossChainServerAppService, IUserLiquidityInfoAppService
{
    private readonly IUserLiquidityRepository _userLiquidityRepository;
    private readonly INESTRepository<UserLiquidityInfoIndex, Guid> _userLiquidityInfoIndexRepository;
    private readonly ITokenRepository _tokenRepository;

    public UserLiquidityInfoAppService(IUserLiquidityRepository userLiquidityRepository,
        INESTRepository<UserLiquidityInfoIndex, Guid> userLiquidityInfoIndexRepository,
        ITokenRepository tokenRepository)
    {
        _userLiquidityRepository = userLiquidityRepository;
        _userLiquidityInfoIndexRepository = userLiquidityInfoIndexRepository;
        _tokenRepository = tokenRepository;
    }

    public async Task<List<UserLiquidityIndexDto>> GetUserLiquidityInfosAsync(GetUserLiquidityInput input)
    {
        var mustQuery =
            new List<Func<QueryContainerDescriptor<UserLiquidityInfoIndex>, QueryContainer>>();
        var providers = new List<string>();
        foreach (var provider in input.Providers.Distinct())
        {
            if (!Base58CheckEncoding.Verify(provider))
            {
                if (Nethereum.Util.AddressExtensions.IsValidEthereumAddressHexFormat(provider))
                {
                    providers.Add(provider.ToLower());
                    continue; 
                }
            }

            if (TonAddressHelper.IsTonFriendlyAddress(provider))
            {
                providers.Add(TonAddressHelper.GetTonRawAddress(provider));
                continue; 
            }
            
            providers.Add(provider);
        }

        mustQuery.Add(q => q.Terms(t => t.Field(f => f.Provider).Terms(providers)));

        if (!string.IsNullOrWhiteSpace(input.ChainId))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        }

        if (!string.IsNullOrWhiteSpace(input.Symbol))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenInfo.Symbol).Value(input.Symbol)));
        }

        if (!string.IsNullOrWhiteSpace(input.TokenAddress))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenInfo.Address).Value(input.TokenAddress)));
        }


        QueryContainer Filter(QueryContainerDescriptor<UserLiquidityInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _userLiquidityInfoIndexRepository.GetListAsync(Filter, sortExp: o => o.Liquidity,
            sortType: SortOrder.Descending);
        return ObjectMapper.Map<List<UserLiquidityInfoIndex>, List<UserLiquidityIndexDto>>(list.Item2);
    }

    public async Task AddUserLiquidityAsync(UserLiquidityInfoInput input)
    {
        var liquidityInfo = await FindUserLiquidityInfoAsync(input.ChainId, input.TokenId, input.Provider);
        var isLiquidityExist = true;
        if (liquidityInfo == null)
        {
            Log.ForContext("ChainId", input.ChainId)
                .ForContext("TokenId", input.TokenId)
                .Debug("New user liquidity info, provider: {provider}", input.Provider);
            isLiquidityExist = false;
            liquidityInfo = ObjectMapper.Map<UserLiquidityInfoInput, UserLiquidityInfo>(input);
        }
        else
        {
            Log.ForContext("ChainId", input.ChainId)
                .ForContext("TokenId", input.TokenId)
                .Debug("Update pool liquidity info, provider: {provider}", input.Provider);
            liquidityInfo.Liquidity += input.Liquidity;
        }

        if (isLiquidityExist)
        {
            await _userLiquidityRepository.UpdateAsync(liquidityInfo, autoSave: true);
        }
        else
        {
            await _userLiquidityRepository.InsertAsync(liquidityInfo, autoSave: true);
        }
    }
    
    public async Task RemoveUserLiquidityAsync(UserLiquidityInfoInput input)
    {
        var liquidityInfo = await FindUserLiquidityInfoAsync(input.ChainId, input.TokenId, input.Provider);
        if (liquidityInfo == null)
        {
            Log.ForContext("ChainId", input.ChainId)
                .ForContext("TokenId", input.TokenId)
                .Error("User liquidity info not found, provider: {provider}", input.Provider);
        }
        else
        {
            liquidityInfo.Liquidity -= input.Liquidity;
            await _userLiquidityRepository.UpdateAsync(liquidityInfo, autoSave: true);
        }
    }

    public async Task AddIndexAsync(AddUserLiquidityInfoIndexInput input)
    {
        var index = ObjectMapper.Map<AddUserLiquidityInfoIndexInput, UserLiquidityInfoIndex>(input);

        if (input.TokenId != Guid.Empty)
        {
            index.TokenInfo = await _tokenRepository.GetAsync(input.TokenId);
        }

        await _userLiquidityInfoIndexRepository.AddAsync(index);
    }

    public async Task UpdateIndexAsync(UpdateUserLiquidityInfoIndexInput input)
    {
        var index = ObjectMapper.Map<UpdateUserLiquidityInfoIndexInput, UserLiquidityInfoIndex>(input);

        if (input.TokenId != Guid.Empty)
        {
            index.TokenInfo = await _tokenRepository.GetAsync(input.TokenId);
        }

        await _userLiquidityInfoIndexRepository.UpdateAsync(index);
    }

    private async Task<UserLiquidityInfo> FindUserLiquidityInfoAsync(string chainId, Guid tokenId, string provider)
    {
        return await _userLiquidityRepository.FindAsync(o =>
            o.ChainId == chainId && o.TokenId == tokenId && o.Provider == provider);
    }
}