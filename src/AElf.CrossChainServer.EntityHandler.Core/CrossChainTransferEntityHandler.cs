using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.EntityHandler.Core
{
    public class CrossChainTransferEntityHandler : ITransientDependency,
        IDistributedEventHandler<EntityCreatedEto<CrossChainTransferEto>>,
        IDistributedEventHandler<EntityUpdatedEto<CrossChainTransferEto>>,
        IDistributedEventHandler<EntityDeletedEto<CrossChainTransferEto>>

    {
        private readonly ICrossChainTransferAppService _crossChainTransferAppService;
        private readonly IObjectMapper _objectMapper;

        public CrossChainTransferEntityHandler(ICrossChainTransferAppService crossChainTransferAppService,
            IObjectMapper objectMapper)
        {
            _crossChainTransferAppService = crossChainTransferAppService;
            _objectMapper = objectMapper;
        }

        public async Task HandleEventAsync(EntityCreatedEto<CrossChainTransferEto> eventData)
        {
            var input = _objectMapper.Map<CrossChainTransferEto, AddCrossChainTransferIndexInput>(eventData.Entity);
            await _crossChainTransferAppService.AddIndexAsync(input);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<CrossChainTransferEto> eventData)
        {
            var input = _objectMapper.Map<CrossChainTransferEto, UpdateCrossChainTransferIndexInput>(eventData.Entity);
            await _crossChainTransferAppService.UpdateIndexAsync(input);
        }

        public async Task HandleEventAsync(EntityDeletedEto<CrossChainTransferEto> eventData)
        {
            await _crossChainTransferAppService.DeleteIndexAsync(eventData.Entity.Id);
        }
    }
}