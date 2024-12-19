using System;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.TokenPool;

public interface IUserLiquidityRepository : IRepository<UserLiquidityInfo, Guid>
{
    
}