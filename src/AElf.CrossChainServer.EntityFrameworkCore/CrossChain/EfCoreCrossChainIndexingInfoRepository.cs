using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.CrossChain
{
    public class EfCoreCrossChainIndexingInfoRepository : EfCoreRepository<CrossChainServerDbContext, CrossChainIndexingInfo, Guid>, ICrossChainIndexingInfoRepository
    {
        public EfCoreCrossChainIndexingInfoRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }
    }
}