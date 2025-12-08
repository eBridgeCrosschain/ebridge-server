using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;
using Microsoft.Extensions.Configuration;


namespace AElf.CrossChainServer.Settings;

public class SettingDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly ISettingManager _settingManager;

    public SettingDataSeedContributor(IConfiguration configuration, ISettingManager settingManager)
    {
        _configuration = configuration;
        _settingManager = settingManager;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        var configurationSection = _configuration.GetSection("IndexerSettings");
        foreach (var section in configurationSection.GetChildren())
        {
            var typePrefix = section.GetValue<string>("TypePrefix");
            var syncType = section.GetValue<string>("SyncType");
            var chainId = section.GetValue<string>("ChainId");
            var value = section.GetValue<string>("Value");
            var settingKey = GetSettingKey(typePrefix, syncType);
            await _settingManager.SetAsync(chainId, settingKey, value);
        }
    }
    
    private string GetSettingKey(string typePrefix,string syncType)
    {
        return string.IsNullOrWhiteSpace(typePrefix)? syncType : $"{typePrefix}-{syncType}";
    }
}