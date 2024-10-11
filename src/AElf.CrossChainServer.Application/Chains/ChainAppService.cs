using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.ExceptionHandler;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace AElf.CrossChainServer.Chains
{
    [RemoteService(IsEnabled = false)]
    public class ChainAppService : CrossChainServerAppService, IChainAppService
    {
        private readonly IChainRepository _chainRepository;
        private readonly INESTRepository<ChainIndex, string> _chainIndexRepository;

        public ChainAppService(IChainRepository chainRepository,
            INESTRepository<ChainIndex, string> chainIndexRepository)
        {
            _chainRepository = chainRepository;
            _chainIndexRepository = chainIndexRepository;
        }

        [ExceptionHandler(typeof(Exception), typeof(EntityNotFoundException),Message = "Chain not found.",
            TargetType = typeof(ExceptionHandlingService),
            MethodName = nameof(ExceptionHandlingService.HandleException))]
        public async Task<ChainDto> GetAsync(string id)
        {
            var chain = await _chainRepository.GetAsync(id);
            return ObjectMapper.Map<Chain, ChainDto>(chain);
        }

        public async Task<ChainDto> GetByNameAsync(string name)
        {
            var chain = await _chainRepository.FindAsync(o => o.Name == name);
            return ObjectMapper.Map<Chain, ChainDto>(chain);
        }

        public async Task<ChainDto> GetByAElfChainIdAsync(int aelfChainId)
        {
            var chain = await _chainRepository.FindAsync(o => o.AElfChainId == aelfChainId);
            return ObjectMapper.Map<Chain, ChainDto>(chain);
        }

        public async Task<ListResultDto<ChainDto>> GetListAsync(GetChainsInput input)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<ChainIndex>, QueryContainer>>();
            if (input.Type.HasValue)
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.Type).Value(input.Type.Value)));
            }

            QueryContainer Filter(QueryContainerDescriptor<ChainIndex> f) => f.Bool(b => b.Must(mustQuery));

            var list = await _chainIndexRepository.GetListAsync(Filter);

            return new ListResultDto<ChainDto>
            {
                Items = ObjectMapper.Map<List<ChainIndex>, List<ChainDto>>(list.Item2)
            };
        }
    }
}