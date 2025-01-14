using System.Threading.Tasks;
using AElf.CrossChainServer.TokenAccess;
using AElf.CrossChainServer.TokenPool;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.EntityHandler.Core;

public class TokenApplyOrderEntityHandler : ITransientDependency,
    IDistributedEventHandler<EntityCreatedEto<TokenApplyOrderEto>>,
    IDistributedEventHandler<EntityUpdatedEto<TokenApplyOrderEto>>
{
    private readonly ITokenAccessAppService _tokenAccessAppService;
    private readonly IObjectMapper _objectMapper;

    public TokenApplyOrderEntityHandler(
        IObjectMapper objectMapper, ITokenAccessAppService tokenAccessAppService)
    {
        _objectMapper = objectMapper;
        _tokenAccessAppService = tokenAccessAppService;
    }


    public async Task HandleEventAsync(EntityCreatedEto<TokenApplyOrderEto> eventData)
    {
        var input = _objectMapper.Map<TokenApplyOrderEto, AddTokenApplyOrderIndexInput>(eventData.Entity);
        await _tokenAccessAppService.AddTokenApplyOrderIndexAsync(input);
    }

    public async Task HandleEventAsync(EntityUpdatedEto<TokenApplyOrderEto> eventData)
    {
        var input = _objectMapper.Map<TokenApplyOrderEto, UpdateTokenApplyOrderIndexInput>(eventData.Entity);
        await _tokenAccessAppService.UpdateTokenApplyOrderIndexAsync(input);
    }
}