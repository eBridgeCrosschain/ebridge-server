using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.EntityHandler.Core
{
    public class CrossChainIndexingInfoEntityHandler : ITransientDependency,
        IDistributedEventHandler<EntityCreatedEto<CrossChainIndexingInfoEto>>,
        IDistributedEventHandler<EntityDeletedEto<CrossChainIndexingInfoEto>>
    {
        private readonly ICrossChainIndexingInfoAppService _crossChainIndexingInfoAppService;
        private readonly IObjectMapper _objectMapper;

        public CrossChainIndexingInfoEntityHandler(
            IObjectMapper objectMapper, ICrossChainIndexingInfoAppService crossChainIndexingInfoAppService)
        {
            _objectMapper = objectMapper;
            _crossChainIndexingInfoAppService = crossChainIndexingInfoAppService;
        }

        public async Task HandleEventAsync(EntityCreatedEto<CrossChainIndexingInfoEto> eventData)
        {
            var input = _objectMapper.Map<CrossChainIndexingInfoEto, AddCrossChainIndexingInfoIndexInput>(eventData.Entity);
            await _crossChainIndexingInfoAppService.AddIndexAsync(input);
        }

        public async Task HandleEventAsync(EntityDeletedEto<CrossChainIndexingInfoEto> eventData)
        {
            await _crossChainIndexingInfoAppService.DeleteIndexAsync(eventData.Entity.Id);
        }
    }
}