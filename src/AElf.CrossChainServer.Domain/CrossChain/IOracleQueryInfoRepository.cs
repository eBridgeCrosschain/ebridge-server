using System;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.CrossChain
{
    public interface IOracleQueryInfoRepository : IRepository<OracleQueryInfo, Guid>
    {
        
    }
}