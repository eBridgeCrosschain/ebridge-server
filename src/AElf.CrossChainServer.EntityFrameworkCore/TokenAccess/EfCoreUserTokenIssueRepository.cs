using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.TokenAccess;

public class EfCoreUserTokenIssueRepository : EfCoreRepository<CrossChainServerDbContext, UserTokenIssueDto, Guid>,
    IUserTokenIssueRepository
{
    public EfCoreUserTokenIssueRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }
}