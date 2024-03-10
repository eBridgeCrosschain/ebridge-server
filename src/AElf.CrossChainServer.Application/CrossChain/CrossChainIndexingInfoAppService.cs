using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.Indexing.Elasticsearch;
using Nest;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.CrossChain;

[RemoteService(IsEnabled = false)]
public class CrossChainIndexingInfoAppService : CrossChainServerAppService, ICrossChainIndexingInfoAppService
{
    private readonly IChainAppService _chainAppService;
    private readonly ICrossChainIndexingInfoRepository _crossChainIndexingInfoRepository;
    private readonly INESTRepository<CrossChainIndexingInfoIndex, Guid> _crossChainIndexingInfoIndexRepository;
    private readonly IBlockchainAppService _blockchainAppService;

    public CrossChainIndexingInfoAppService(ICrossChainIndexingInfoRepository crossChainIndexingInfoRepository,
        IChainAppService chainAppService,
        INESTRepository<CrossChainIndexingInfoIndex, Guid> crossChainIndexingInfoIndexRepository,
        IBlockchainAppService blockchainAppService)
    {
        _crossChainIndexingInfoRepository = crossChainIndexingInfoRepository;
        _chainAppService = chainAppService;
        _crossChainIndexingInfoIndexRepository = crossChainIndexingInfoIndexRepository;
        _blockchainAppService = blockchainAppService;
    }

    public async Task CreateAsync(CreateCrossChainIndexingInfoInput input)
    {
        if (await _crossChainIndexingInfoRepository.FirstOrDefaultAsync(o =>
                o.ChainId == input.ChainId && o.IndexChainId == input.IndexChainId &&
                o.IndexBlockHeight == input.IndexBlockHeight) != null)
        {
            return;
        }

        var index = ObjectMapper.Map<CreateCrossChainIndexingInfoInput, CrossChainIndexingInfo>(input);
        await _crossChainIndexingInfoRepository.InsertAsync(index);
    }

    public async Task CleanAsync(DateTime time)
    {
        await _crossChainIndexingInfoRepository.DeleteAsync(o => o.BlockTime < time);
    }

    public async Task AddIndexAsync(AddCrossChainIndexingInfoIndexInput input)
    {
        var index = ObjectMapper.Map<AddCrossChainIndexingInfoIndexInput, CrossChainIndexingInfoIndex>(input);
        await _crossChainIndexingInfoIndexRepository.AddAsync(index);
    }

    public async Task DeleteIndexAsync(Guid id)
    {
        await _crossChainIndexingInfoIndexRepository.DeleteAsync(id);
    }

    public async Task<int> CalculateCrossChainProgressAsync(string fromChainId, string toChainId, long height)
    {
        var fromChain = await _chainAppService.GetAsync(fromChainId);
        var toChain = await _chainAppService.GetAsync(toChainId);
        if (fromChain == null || toChain == null)
        {
            throw new UserFriendlyException("parameter chainId is not registered in database!");
        }
        if (fromChain.Type != BlockchainType.AElf || toChain.Type != BlockchainType.AElf)
        {
            throw new UserFriendlyException("parameter chainId is not valid!");
        }
        
        var block = await _blockchainAppService.GetBlockByHeightAsync(fromChainId, height);
        return await CalculateCrossChainProgressAsync(fromChainId, toChainId, height, block.Header.Time);
    }

    public async Task<int> CalculateCrossChainProgressAsync(string fromChainId, string toChainId, long height,
        DateTime txTime)
    {
        var fromChain = await _chainAppService.GetAsync(fromChainId);
        var toChain = await _chainAppService.GetAsync(toChainId);
        
        if (fromChain == null || toChain == null)
        {
            throw new UserFriendlyException("parameter chainId is not registered in database!");
        }

        if (fromChain.IsMainChain)
        {
            return await CalculateAElfProgressAsync(fromChain, toChain, height, txTime);
        }

        var mainChain = await _chainAppService.GetAsync(CrossChainServerConsts.AElfMainChainId);

        var mustQuery = new List<Func<QueryContainerDescriptor<CrossChainIndexingInfoIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId).Value(mainChain.Id)),
            q => q.Term(i => i.Field(f => f.IndexChainId).Value(fromChain.Id)),
            q => q.LongRange(i => i.Field(f => f.IndexBlockHeight).GreaterThanOrEquals(height))
        };

        QueryContainer Query(QueryContainerDescriptor<CrossChainIndexingInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var mainChainIndex =
            await _crossChainIndexingInfoIndexRepository.GetAsync(Query, sortExp: o => o.BlockTime);

        if (mainChainIndex == null)
        {
            return await CalculateAElfProgressAsync(fromChain, mainChain, height, txTime) / 2;
        }
        if (toChain.IsMainChain)
        {
            return CrossChainServerConsts.HalfOfTheProgress + await CalculateAElfProgressAsync(mainChain, fromChain, mainChainIndex.BlockHeight,
                mainChainIndex.BlockTime) / 2;
        }

        var sideChainIndexProgress = await CalculateAElfProgressAsync(mainChain, fromChain,
            mainChainIndex.BlockHeight, mainChainIndex.BlockTime);
        return CrossChainServerConsts.HalfOfTheProgress + (sideChainIndexProgress == CrossChainServerConsts.FullOfTheProgress
            ? await CalculateAElfProgressAsync(mainChain, toChain, mainChainIndex.BlockHeight,
                mainChainIndex.BlockTime) / 2
            : 0);
    }

    private async Task<int> CalculateAElfProgressAsync(ChainDto fromChain, ChainDto toChain, long txHeight,
        DateTime txTime)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CrossChainIndexingInfoIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId).Value(toChain.Id)),
            q => q.Term(i => i.Field(f => f.IndexChainId).Value(fromChain.Id))
        };
        QueryContainer CurrentIndexedFilter(QueryContainerDescriptor<CrossChainIndexingInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        var currentIndexed =
            await _crossChainIndexingInfoIndexRepository.GetAsync(CurrentIndexedFilter, sortExp: o => o.BlockTime, sortType: SortOrder.Descending);
        
        if (currentIndexed == null)
        {
            return 0;
        }

        var currentIndexedHeight = currentIndexed.IndexBlockHeight;
        
        mustQuery = new List<Func<QueryContainerDescriptor<CrossChainIndexingInfoIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId).Value(toChain.Id)),
            q => q.Term(i => i.Field(f => f.IndexChainId).Value(fromChain.Id)),
            q => q.DateRange(i => i.Field(f => f.BlockTime).LessThan(txTime))
        };
        QueryContainer TransferIndexedQuery(QueryContainerDescriptor<CrossChainIndexingInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        var transferIndexed =
            await _crossChainIndexingInfoIndexRepository.GetAsync(TransferIndexedQuery, sortExp: o => o.BlockTime, sortType: SortOrder.Descending);

        if (transferIndexed == null || currentIndexedHeight >= txHeight)
        {
            return CrossChainServerConsts.FullOfTheProgress;
        }

        var transferIndexedHeight = transferIndexed.IndexBlockHeight;

        return (int) ((currentIndexedHeight - transferIndexedHeight) * CrossChainServerConsts.FullOfTheProgress /
                      (txHeight - transferIndexedHeight));
    }
}