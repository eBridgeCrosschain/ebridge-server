using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;
using Nest;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenPool;

[RemoteService(IsEnabled = false)]
public class PoolLiquidityInfoAppService : CrossChainServerAppService, IPoolLiquidityInfoAppService
{
    private readonly IPoolLiquidityRepository _poolLiquidityRepository;
    private readonly INESTRepository<PoolLiquidityInfoIndex, Guid> _poolLiquidityInfoIndexRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IChainAppService _chainAppService;

    public PoolLiquidityInfoAppService(IPoolLiquidityRepository poolLiquidityRepository,
        INESTRepository<PoolLiquidityInfoIndex, Guid> poolLiquidityInfoIndexRepository,
        ITokenRepository tokenRepository, IChainAppService chainAppService)
    {
        _poolLiquidityRepository = poolLiquidityRepository;
        _poolLiquidityInfoIndexRepository = poolLiquidityInfoIndexRepository;
        _tokenRepository = tokenRepository;
        _chainAppService = chainAppService;
    }

    public async Task<PagedResultDto<PoolLiquidityIndexDto>> GetPoolLiquidityInfosAsync(GetPoolLiquidityInfosInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PoolLiquidityInfoIndex>, QueryContainer>>();
        if (input.ChainId == null)
        {
            var chainList = await _chainAppService.GetListAsync(new GetChainsInput());
            foreach (var chain in chainList.Items)
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chain.Id)));
            }
        }else if (!string.IsNullOrWhiteSpace(input.ChainId))
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
        QueryContainer Filter(QueryContainerDescriptor<PoolLiquidityInfoIndex> f) => f.Bool(b => b.Must(mustQuery));

        var list = await _poolLiquidityInfoIndexRepository.GetListAsync(Filter, limit: input.MaxResultCount,
            skip: input.SkipCount,sortExp: o => o.Liquidity,
            sortType: SortOrder.Descending);
        var totalCount = await _poolLiquidityInfoIndexRepository.CountAsync(Filter);

        return new PagedResultDto<PoolLiquidityIndexDto>
        {
            TotalCount = totalCount.Count,
            Items = ObjectMapper.Map<List<PoolLiquidityInfoIndex>, List<PoolLiquidityIndexDto>>(list.Item2)
        };
    }

    public async Task AddLiquidityAsync(PoolLiquidityInfoInput input)
    {
        var liquidityInfo = await FindPoolLiquidityInfoAsync(input.ChainId, input.TokenId);
        var isLiquidityExist = true;
        if (liquidityInfo == null)
        {
            Log.ForContext("ChainId", input.ChainId)
                .ForContext("TokenId", input.TokenId)
                .Debug("New pool liquidity info");
            isLiquidityExist = false;
            liquidityInfo = ObjectMapper.Map<PoolLiquidityInfoInput, PoolLiquidityInfo>(input);
        }
        else
        {
            Log.ForContext("ChainId", input.ChainId)
                .ForContext("TokenId", input.TokenId)
                .Debug("Update pool liquidity info");
            liquidityInfo.Liquidity += input.Liquidity;
        }

        if (isLiquidityExist)
        {
            await _poolLiquidityRepository.UpdateAsync(liquidityInfo, autoSave: true);
        }
        else
        {
            await _poolLiquidityRepository.InsertAsync(liquidityInfo, autoSave: true);
        }
    }

    public async Task RemoveLiquidityAsync(PoolLiquidityInfoInput input)
    {
        var liquidityInfo = await FindPoolLiquidityInfoAsync(input.ChainId, input.TokenId);
        if (liquidityInfo == null)
        {
            Log.ForContext("ChainId", input.ChainId)
                .ForContext("TokenId", input.TokenId)
                .Error("Pool liquidity info not found");
        }
        else
        {
            liquidityInfo.Liquidity -= input.Liquidity;
            await _poolLiquidityRepository.UpdateAsync(liquidityInfo, autoSave: true);
        }
    }

    public async Task AddIndexAsync(AddPoolLiquidityInfoIndexInput input)
    {
        var index = ObjectMapper.Map<AddPoolLiquidityInfoIndexInput, PoolLiquidityInfoIndex>(input);

        if (input.TokenId != Guid.Empty)
        {
            index.TokenInfo = await _tokenRepository.GetAsync(input.TokenId);
        }

        await _poolLiquidityInfoIndexRepository.AddAsync(index);
    }

    public async Task UpdateIndexAsync(UpdatePoolLiquidityInfoIndexInput input)
    {
        var index = ObjectMapper.Map<UpdatePoolLiquidityInfoIndexInput, PoolLiquidityInfoIndex>(input);

        if (input.TokenId != Guid.Empty)
        {
            index.TokenInfo = await _tokenRepository.GetAsync(input.TokenId);
        }

        await _poolLiquidityInfoIndexRepository.UpdateAsync(index);
    }

    public Task SyncPoolLiquidityInfoFromChainAsync()
    {
        throw new NotImplementedException();
    }

    private async Task<PoolLiquidityInfo> FindPoolLiquidityInfoAsync(string chainId, Guid tokenId)
    {
        return await _poolLiquidityRepository.FindAsync(o => o.ChainId == chainId && o.TokenId == tokenId);
    }
}