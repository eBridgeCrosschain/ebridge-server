using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.TokenPool;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.EntityHandler.Core;

public class PoolLiquidityEntityHandler: ITransientDependency,
    IDistributedEventHandler<EntityCreatedEto<PoolLiquidityEto>>,
    IDistributedEventHandler<EntityUpdatedEto<PoolLiquidityEto>>
{
    private readonly IPoolLiquidityInfoAppService _poolLiquidityInfoAppService;
    private readonly IObjectMapper _objectMapper;

    public PoolLiquidityEntityHandler(IPoolLiquidityInfoAppService poolLiquidityInfoAppService,
        IObjectMapper objectMapper)
    {
        _poolLiquidityInfoAppService = poolLiquidityInfoAppService;
        _objectMapper = objectMapper;
    }

    public async Task HandleEventAsync(EntityCreatedEto<PoolLiquidityEto> eventData)
    {
        var input = _objectMapper.Map<PoolLiquidityEto, AddPoolLiquidityInfoIndexInput>(eventData.Entity);
        await _poolLiquidityInfoAppService.AddIndexAsync(input);
    }

    public async Task HandleEventAsync(EntityUpdatedEto<PoolLiquidityEto> eventData)
    {
        var input = _objectMapper.Map<PoolLiquidityEto, UpdatePoolLiquidityInfoIndexInput>(eventData.Entity);
        await _poolLiquidityInfoAppService.UpdateIndexAsync(input);
    }
}