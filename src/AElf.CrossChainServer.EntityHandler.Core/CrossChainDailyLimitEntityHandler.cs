using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.EntityHandler.Core
{
    public class CrossChainDailyLimitEntityHandler : ITransientDependency,
        IDistributedEventHandler<EntityCreatedEto<CrossChainDailyLimitEto>>,
        IDistributedEventHandler<EntityUpdatedEto<CrossChainDailyLimitEto>>
    {
        private readonly ICrossChainLimitAppService _crossChainLimitAppService;
        private readonly IObjectMapper _objectMapper;

        public CrossChainDailyLimitEntityHandler(
            IObjectMapper objectMapper, ICrossChainLimitAppService crossChainLimitAppService)
        {
            _objectMapper = objectMapper;
            _crossChainLimitAppService = crossChainLimitAppService;
        }

        public async Task HandleEventAsync(EntityCreatedEto<CrossChainDailyLimitEto> eventData)
        {
            var input = _objectMapper.Map<CrossChainDailyLimitEto, SetCrossChainDailyLimitInput>(eventData.Entity);
            await _crossChainLimitAppService.SetCrossChainDailyLimitIndexAsync(input);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<CrossChainDailyLimitEto> eventData)
        {
            var input = _objectMapper.Map<CrossChainDailyLimitEto, SetCrossChainDailyLimitInput>(eventData.Entity);
            await _crossChainLimitAppService.SetCrossChainDailyLimitIndexAsync(input);
        }
    }
}