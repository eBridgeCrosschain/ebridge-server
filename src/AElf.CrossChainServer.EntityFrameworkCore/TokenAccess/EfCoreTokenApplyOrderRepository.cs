using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.TokenAccess;

public class EfCoreTokenApplyOrderRepository : EfCoreRepository<CrossChainServerDbContext, TokenApplyOrder, Guid>,
    ITokenApplyOrderRepository
{
    public EfCoreTokenApplyOrderRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }
}