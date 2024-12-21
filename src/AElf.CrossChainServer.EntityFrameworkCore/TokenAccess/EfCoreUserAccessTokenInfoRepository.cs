using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.TokenAccess;

public class EfCoreUserAccessTokenInfoRepository : EfCoreRepository<CrossChainServerDbContext, UserTokenAccessInfo, Guid>,
    IUserAccessTokenInfoRepository
{
    public EfCoreUserAccessTokenInfoRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }
}