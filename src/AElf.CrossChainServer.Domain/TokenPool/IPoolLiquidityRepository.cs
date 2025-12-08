using System;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.TokenPool;

public interface IPoolLiquidityRepository : IRepository<PoolLiquidityInfo, Guid>
{
    
}