using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.TokenPool;

public class EfCorePoolLiquidityRepository : EfCoreRepository<CrossChainServerDbContext, PoolLiquidityInfo, Guid>, IPoolLiquidityRepository
{
    public EfCorePoolLiquidityRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider)
        : base(dbContextProvider)
    {

    }
}