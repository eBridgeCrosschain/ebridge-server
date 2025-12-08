using System;
using AElf.CrossChainServer.TokenAccess.UserTokenAccess;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.TokenAccess;

public interface IUserAccessTokenInfoRepository : IRepository<UserTokenAccessInfo, Guid>
{
}