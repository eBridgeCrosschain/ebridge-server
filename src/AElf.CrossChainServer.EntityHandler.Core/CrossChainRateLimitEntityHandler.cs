using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.EntityHandler.Core
{
    public class CrossChainRateLimitEntityHandler : ITransientDependency,
        IDistributedEventHandler<EntityCreatedEto<CrossChainRateLimitEto>>,
        IDistributedEventHandler<EntityUpdatedEto<CrossChainRateLimitEto>>
    {
        private readonly ICrossChainLimitAppService _crossChainLimitAppService;
        private readonly IObjectMapper _objectMapper;

        public CrossChainRateLimitEntityHandler(
            IObjectMapper objectMapper, ICrossChainLimitAppService crossChainLimitAppService)
        {
            _objectMapper = objectMapper;
            _crossChainLimitAppService = crossChainLimitAppService;
        }

        public async Task HandleEventAsync(EntityCreatedEto<CrossChainRateLimitEto> eventData)
        {
            var input = _objectMapper.Map<CrossChainRateLimitEto, SetCrossChainRateLimitInput>(eventData.Entity);
            await _crossChainLimitAppService.SetCrossChainRateLimitIndexAsync(input);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<CrossChainRateLimitEto> eventData)
        {
            var input = _objectMapper.Map<CrossChainRateLimitEto, SetCrossChainRateLimitInput>(eventData.Entity);
            await _crossChainLimitAppService.SetCrossChainRateLimitIndexAsync(input);
        }
    }
}