using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.TokenAccess;

public class EfCoreThirdUserTokenIssueRepository : EfCoreRepository<CrossChainServerDbContext, ThirdUserTokenIssueInfo, Guid>,
    IThirdUserTokenIssueRepository
{
    public EfCoreThirdUserTokenIssueRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider) : base(
        dbContextProvider)
    {
    }
}