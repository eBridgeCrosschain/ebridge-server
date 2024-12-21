using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.TokenAccess;

public class EfCoreTokenInvokeRepository : EfCoreRepository<CrossChainServerDbContext, TokenInvokeDto, Guid>,
    ITokenInvokeRepository
{
    public EfCoreTokenInvokeRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }
}