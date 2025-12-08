using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.CrossChain;

public class EfCoreCrossChainUserRepository : EfCoreRepository<CrossChainServerDbContext, WalletUserDto, Guid>,
    ICrossChainUserRepository
{
    public EfCoreCrossChainUserRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }
}