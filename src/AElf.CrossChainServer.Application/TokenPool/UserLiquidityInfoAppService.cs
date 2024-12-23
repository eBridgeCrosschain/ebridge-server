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
    private readonly IChainAppService _chainAppService;
    private readonly ITokenApplyOrderRepository _tokenApplyOrderRepository;
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly TokenPriceIdMappingOptions _tokenPriceIdMappingOptions;
    private readonly TokenAccessOptions _tokenAccessOptions;

    public UserLiquidityInfoAppService(IUserLiquidityRepository userLiquidityRepository,
        INESTRepository<UserLiquidityInfoIndex, Guid> userLiquidityInfoIndexRepository,
        ITokenRepository tokenRepository, IChainAppService chainAppService,
        ITokenApplyOrderRepository tokenApplyOrderRepository, ITokenPriceProvider tokenPriceProvider,
        IOptionsSnapshot<TokenPriceIdMappingOptions> tokenPriceIdMappingOptions,
        IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions)
    {
        _userLiquidityRepository = userLiquidityRepository;
        _userLiquidityInfoIndexRepository = userLiquidityInfoIndexRepository;
        _tokenRepository = tokenRepository;
        _chainAppService = chainAppService;
        _tokenApplyOrderRepository = tokenApplyOrderRepository;
        _tokenPriceProvider = tokenPriceProvider;
        _tokenPriceIdMappingOptions = tokenPriceIdMappingOptions.Value;
        _tokenAccessOptions = tokenAccessOptions.Value;
    }

    public async Task<List<UserLiquidityIndexDto>> GetUserLiquidityInfosAsync(GetUserLiquidityInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserLiquidityInfoIndex>, QueryContainer>>
            { q => q.Term(i => i.Field(f => f.Provider).Value(input.Provider)) };
        if (!string.IsNullOrWhiteSpace(input.ChainId))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
            if (!string.IsNullOrWhiteSpace(input.Token))
            {
                var chain = await _chainAppService.GetAsync(input.ChainId);
                switch (chain.Type)
                {
                    case BlockchainType.AElf:
                        mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenInfo.Symbol).Value(input.Token)));
                        break;
                    case BlockchainType.Evm:
                        mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenInfo.Address).Value(input.Token)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
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
            await DealUpdateOrderInfoAsync(liquidityInfo.Provider, liquidityInfo.ChainId, liquidityInfo.TokenId,
                liquidityInfo.Liquidity);
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

    private async Task DealUpdateOrderInfoAsync(string provider, string chainId, Guid tokenId, decimal amount)
    {
        var token = await _tokenRepository.GetAsync(tokenId);
        var amountInUsd =
            await _tokenPriceProvider.GetPriceAsync(_tokenPriceIdMappingOptions.CoinIdMapping[token.Symbol]);
        if (amountInUsd < _tokenAccessOptions.DefaultConfig.MinLiquidityInUsd)
        {
            Log.Warning(
                "Amount is less than min liquidity in usd in the first time, provider: {provider}, amount: {amount}",
                provider, amount);
            return;
        }

        var order = await _tokenApplyOrderRepository.FindAsync(o =>
            o.UserAddress == provider && o.Symbol == token.Symbol);
        if (order == null)
        {
            Log.ForContext("ChainId", chainId)
                .ForContext("TokenId", tokenId)
                .Warning("Token apply order not found, provider: {provider}", provider);
        }
        else
        {
            foreach (var chainTokenInfo in order.ChainTokenInfo.Where(chainTokenInfo => chainTokenInfo.ChainId == chainId &&
                         chainTokenInfo.Status == TokenApplyOrderStatus.LiquidityAdded.ToString()))
            {
                chainTokenInfo.Status = TokenApplyOrderStatus.Complete.ToString();
            }
            if (order.ChainTokenInfo.All(o => o.Status == TokenApplyOrderStatus.Complete.ToString()))
            {
                order.Status = TokenApplyOrderStatus.Complete.ToString();
            }
            await _tokenApplyOrderRepository.UpdateAsync(order, autoSave: true);
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