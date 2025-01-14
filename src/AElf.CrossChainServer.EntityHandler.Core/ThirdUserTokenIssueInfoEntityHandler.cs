using System.Threading.Tasks;
using AElf.CrossChainServer.TokenAccess;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.EntityHandler.Core;

public class ThirdUserTokenIssueInfoEntityHandler:ITransientDependency,
    IDistributedEventHandler<EntityCreatedEto<ThirdUserTokenIssueInfoEto>>,
    IDistributedEventHandler<EntityUpdatedEto<ThirdUserTokenIssueInfoEto>>
{
    private readonly ITokenAccessAppService _tokenAccessAppService;
    private readonly IObjectMapper _objectMapper;

    public ThirdUserTokenIssueInfoEntityHandler(ITokenAccessAppService tokenAccessAppService, IObjectMapper objectMapper)
    {
        _tokenAccessAppService = tokenAccessAppService;
        _objectMapper = objectMapper;
    }

    public async Task HandleEventAsync(EntityCreatedEto<ThirdUserTokenIssueInfoEto> eventData)
    {
        var input = _objectMapper.Map<ThirdUserTokenIssueInfoEto, AddThirdUserTokenIssueInfoIndexInput>(eventData.Entity);
        await _tokenAccessAppService.AddThirdUserTokenIssueInfoIndexAsync(input);
    }

    public async Task HandleEventAsync(EntityUpdatedEto<ThirdUserTokenIssueInfoEto> eventData)
    {
        var input = _objectMapper.Map<ThirdUserTokenIssueInfoEto, UpdateThirdUserTokenIssueInfoIndexInput>(eventData.Entity);
        await _tokenAccessAppService.UpdateThirdUserTokenIssueInfoIndexAsync(input);
    }
}