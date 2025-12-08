using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.Settings
{
    public class EfCoreSettingsRepository : EfCoreCacheRepository<CrossChainServerDbContext, Settings, Guid>, ISettingsRepository
    {
        public EfCoreSettingsRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
            
        }
    }
}