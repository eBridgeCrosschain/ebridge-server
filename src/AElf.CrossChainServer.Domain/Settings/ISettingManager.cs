using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.Settings;

public interface ISettingManager
{
    Task<string> GetOrNullAsync(string chainId, string name);
    Task SetAsync(string chainId, string name, string value);
}

public class SettingManager : ISettingManager, ITransientDependency
{
    private readonly ISettingsRepository _settingsRepository;
    public ILogger<SettingManager> Logger { get; set; }


    public SettingManager(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
        Logger = NullLogger<SettingManager>.Instance;

    }

    public async Task<string> GetOrNullAsync(string chainId, string name)
    {
        var settings = await _settingsRepository.FindAsync(o=>o.ChainId == chainId && o.Name == name);
        return settings?.Value;
    }

    public async Task SetAsync(string chainId, string name, string value)
    {
        Logger.LogInformation("Start to set setting.{chainId}-{name}-{value}",chainId,name,value);
        var settings = await _settingsRepository.FindAsync(o=>o.ChainId == chainId && o.Name == name);
        if (settings == null)
        {
            await _settingsRepository.InsertAsync(new Settings
            {
                ChainId = chainId,
                Name = name,
                Value = value
            });
        }
        else
        {
            settings.Value = value;
            await _settingsRepository.UpdateAsync(settings);
        }
    }
}