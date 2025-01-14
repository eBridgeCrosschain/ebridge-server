using System;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;

public interface IThirdUserTokenIssueRepository : IRepository<ThirdUserTokenIssueInfo, Guid>
{
}