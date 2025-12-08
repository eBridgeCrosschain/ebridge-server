using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.CrossChain
{
    public class EfCoreCrossChainRateLimitRepository : EfCoreRepository<CrossChainServerDbContext, CrossChainRateLimit, Guid>, ICrossChainRateLimitRepository
    {
        public EfCoreCrossChainRateLimitRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }
    }
}