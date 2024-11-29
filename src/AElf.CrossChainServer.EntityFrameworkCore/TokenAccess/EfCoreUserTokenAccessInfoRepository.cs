using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.TokenAccess;

public class EfCoreUserTokenAccessInfoRepository : EfCoreRepository<CrossChainServerDbContext, UserTokenAccessInfo, Guid>,
    IUserAccessTokenInfoRepository
{
    public EfCoreUserTokenAccessInfoRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }
}