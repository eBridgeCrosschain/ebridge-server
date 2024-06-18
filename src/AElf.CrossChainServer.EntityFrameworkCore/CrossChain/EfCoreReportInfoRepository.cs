using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.CrossChain
{
    public class EfCoreReportInfoRepository : EfCoreRepository<CrossChainServerDbContext, ReportInfo, Guid>, IReportInfoRepository
    {
        public EfCoreReportInfoRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }
    }
}