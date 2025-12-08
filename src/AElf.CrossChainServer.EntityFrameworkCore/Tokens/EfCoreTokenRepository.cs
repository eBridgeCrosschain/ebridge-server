using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.Tokens
{
    public class EfCoreTokenRepository : EfCoreCacheRepository<CrossChainServerDbContext, Token, Guid>, ITokenRepository
    {
        public EfCoreTokenRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
            
        }
    }
}