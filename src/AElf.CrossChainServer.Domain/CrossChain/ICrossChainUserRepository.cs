using System;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.CrossChain;

public interface ICrossChainUserRepository : IRepository<WalletUserDto, Guid>
{
}