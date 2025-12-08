using System;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.Tokens
{
    public interface ITokenRepository : IRepository<Token, Guid>
    {
        
    }
}