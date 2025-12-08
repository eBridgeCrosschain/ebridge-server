using System;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.CrossChain
{
    public interface ICrossChainDailyLimitRepository : IRepository<CrossChainDailyLimit, Guid>
    {
        
    }
}