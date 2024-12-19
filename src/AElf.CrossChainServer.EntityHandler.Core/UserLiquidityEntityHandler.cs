using System.Threading.Tasks;
using AElf.CrossChainServer.TokenPool;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.EntityHandler.Core;

public class UserLiquidityEntityHandler:ITransientDependency,
    IDistributedEventHandler<EntityCreatedEto<UserLiquidityEto>>,
    IDistributedEventHandler<EntityUpdatedEto<UserLiquidityEto>>
{
    private readonly IUserLiquidityInfoAppService _userLiquidityInfoAppService;
    private readonly IObjectMapper _objectMapper;

    public UserLiquidityEntityHandler(IUserLiquidityInfoAppService userLiquidityInfoAppService,
        IObjectMapper objectMapper)
    {
        _userLiquidityInfoAppService = userLiquidityInfoAppService;
        _objectMapper = objectMapper;
    }

    public async Task HandleEventAsync(EntityCreatedEto<UserLiquidityEto> eventData)
    {
        var input = _objectMapper.Map<UserLiquidityEto, AddUserLiquidityInfoIndexInput>(eventData.Entity);
        await _userLiquidityInfoAppService.AddIndexAsync(input);
    }

    public async Task HandleEventAsync(EntityUpdatedEto<UserLiquidityEto> eventData)
    {
        var input = _objectMapper.Map<UserLiquidityEto, UpdateUserLiquidityInfoIndexInput>(eventData.Entity);
        await _userLiquidityInfoAppService.UpdateIndexAsync(input);
    }
}