using System;
using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.Settings
{
    public interface ISettingsRepository : IRepository<Settings, Guid>
    {
        
    }
}