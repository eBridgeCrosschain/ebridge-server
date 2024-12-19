using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenPool;

public interface IPoolLiquidityInfoAppService
{
    Task<PagedResultDto<PoolLiquidityIndexDto>> GetPoolLiquidityInfosAsync(GetPoolLiquidityInfosInput input);
    Task AddLiquidityAsync(PoolLiquidityInfoInput input);
    Task RemoveLiquidityAsync(PoolLiquidityInfoInput input);
    Task AddIndexAsync(AddPoolLiquidityInfoIndexInput input);
    Task UpdateIndexAsync(UpdatePoolLiquidityInfoIndexInput input);

    Task SyncPoolLiquidityInfoFromChainAsync();
}