using System;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.TokenAccess;

public interface IUserTokenOwnerRepository : IRepository<UserTokenOwnerDto, Guid>
{
}