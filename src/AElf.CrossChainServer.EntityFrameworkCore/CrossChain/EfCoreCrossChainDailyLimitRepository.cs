using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.CrossChain
{
    public class EfCoreCrossChainDailyLimitRepository : EfCoreRepository<CrossChainServerDbContext, CrossChainDailyLimit, Guid>, ICrossChainDailyLimitRepository
    {
        public EfCoreCrossChainDailyLimitRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }
    }
}