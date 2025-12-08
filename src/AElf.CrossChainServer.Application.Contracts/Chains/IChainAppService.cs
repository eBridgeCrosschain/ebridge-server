using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.Chains
{
    public interface IChainAppService
    {
        Task<ListResultDto<ChainDto>> GetListAsync(GetChainsInput input);

        Task<ChainDto> GetAsync(string id);

        Task<ChainDto> GetByNameAsync(string name);

        Task<ChainDto> GetByAElfChainIdAsync(int aelfChainId);
    }
}