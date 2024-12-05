using System.Threading.Tasks;
using AElf.CrossChainServer.TokenAccess;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.EntityHandler.Core;

public class UserTokenAccessInfoEntityHandler : ITransientDependency,
    IDistributedEventHandler<EntityCreatedEto<UserTokenAccessInfoEto>>,
    IDistributedEventHandler<EntityDeletedEto<UserTokenAccessInfoEto>>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ITokenAccessAppService _tokenAccessAppService;

    public UserTokenAccessInfoEntityHandler(IObjectMapper objectMapper, ITokenAccessAppService tokenAccessAppService)
    {
        _objectMapper = objectMapper;
        _tokenAccessAppService = tokenAccessAppService;
    }

    public async Task HandleEventAsync(EntityCreatedEto<UserTokenAccessInfoEto> eventData)
    {
        var input = _objectMapper.Map<UserTokenAccessInfoEto, AddUserTokenAccessInfoIndexInput>(eventData.Entity);
        await _tokenAccessAppService.AddUserTokenAccessInfoIndexAsync(input);
    }

    public async Task HandleEventAsync(EntityDeletedEto<UserTokenAccessInfoEto> eventData)
    {
        var input = _objectMapper.Map<UserTokenAccessInfoEto, UpdateUserTokenAccessInfoIndexInput>(eventData.Entity);
        await _tokenAccessAppService.UpdateUserTokenAccessInfoIndexAsync(input);
    }
}