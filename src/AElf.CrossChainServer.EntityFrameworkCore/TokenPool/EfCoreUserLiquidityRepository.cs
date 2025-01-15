using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.TokenPool;

public class EfCoreUserLiquidityRepository : EfCoreRepository<CrossChainServerDbContext, UserLiquidityInfo, Guid>, IUserLiquidityRepository
{
    public EfCoreUserLiquidityRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider)
        : base(dbContextProvider)
    {

    }
}