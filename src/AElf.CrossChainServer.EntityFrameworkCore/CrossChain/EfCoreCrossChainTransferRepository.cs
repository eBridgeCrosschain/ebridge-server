using System;
using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer.CrossChain
{
    public class EfCoreCrossChainTransferRepository : EfCoreRepository<CrossChainServerDbContext, CrossChainTransfer, Guid>, ICrossChainTransferRepository
    {
        public EfCoreCrossChainTransferRepository(IDbContextProvider<CrossChainServerDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }
    }
}