
using System.Threading.Tasks;
using AElf.CrossChainServer.TokenAccess;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.EntityHandler.Core;

public class TokenApplyOrderEntityHandler : ITransientDependency,
    IDistributedEventHandler<EntityCreatedEto<TokenApplyOrderEto>>,
    IDistributedEventHandler<EntityDeletedEto<TokenApplyOrderEto>>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ITokenAccessAppService _tokenAccessAppService;

    public TokenApplyOrderEntityHandler(IObjectMapper objectMapper, ITokenAccessAppService tokenAccessAppService)
    {
        _objectMapper = objectMapper;
        _tokenAccessAppService = tokenAccessAppService;
    }

    public async Task HandleEventAsync(EntityCreatedEto<TokenApplyOrderEto> eventData)
    {
        var input = _objectMapper.Map<TokenApplyOrderEto, AddTokenApplyOrderIndexInput>(eventData.Entity);
        await _tokenAccessAppService.AddTokenApplyOrderIndexAsync(input);
    }

    public async Task HandleEventAsync(EntityDeletedEto<TokenApplyOrderEto> eventData)
    {
        var input = _objectMapper.Map<TokenApplyOrderEto, UpdateTokenApplyOrderIndexInput>(eventData.Entity);
        await _tokenAccessAppService.UpdateTokenApplyOrderIndexAsync(input);
    }
}