using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.TokenAccess;

public class EfCoreUserTokenOwnerRepository : EfCoreRepository<CrossChainServerDbContext, UserTokenOwner, Guid>,
    IUserTokenOwnerRepository
{
    public EfCoreUserTokenOwnerRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }
}