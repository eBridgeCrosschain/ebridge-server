using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.Chains
{
    public class EfCoreChainRepository : EfCoreCacheRepository<CrossChainServerDbContext, Chain, string>, IChainRepository
    {
        public EfCoreChainRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }
    }
}