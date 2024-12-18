using AElf.CrossChainServer.Auth.DTOs;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.Auth.Provider;

public interface ICrossChainUserRepository : IRepository<UserDto, Guid>
{
}