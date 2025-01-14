using System.Threading.Tasks;
using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenPool;

public interface ILiquidityAppService
{
    Task<PoolOverviewDto> GetPoolOverviewAsync([CanBeNull] string addresses);
    
    Task<PagedResultDto<PoolInfoDto>> GetPoolListAsync(GetPoolListInput input);
    Task<PoolInfoDto> GetPoolDetailAsync(GetPoolDetailInput input);
}