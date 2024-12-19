using System;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.Worker.TokenPoolSync;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public abstract class EvmIndexerSyncProviderBase : IEvmSyncProvider, ITransientDependency
{
    protected readonly ISettingManager SettingManager;
    public IBlockchainAppService BlockchainAppService { get; set; }
    protected const int MaxRequestCount = 1000;

    protected EvmIndexerSyncProviderBase(ISettingManager settingManager)
    {
        SettingManager = settingManager;
    }

    public async Task ExecuteAsync(string chainId)
    {
        var syncHeight = await GetSyncHeightAsync(chainId, null);
        var currentIndexHeight = await GetConfirmedHeightAsync(chainId);
        var endHeight = Math.Min(syncHeight + MaxRequestCount, currentIndexHeight);
        if (endHeight <= syncHeight)
        {
            return;
        }
        Log.ForContext("chainId", chainId).Debug("Start to sync chain {chainId} from {SyncHeight} to {EndHeight}",
            chainId, syncHeight + 1, endHeight);

        var height = await HandleDataAsync(chainId, syncHeight + 1,
            endHeight);

        await SetSyncHeightAsync(chainId, null, height);
    }

    public async Task<long> GetConfirmedHeightAsync(string chainId)
    {
        var chainStatus = await BlockchainAppService.GetChainStatusAsync(chainId);
        return chainStatus.ConfirmedBlockHeight;
    }

    private string GetSettingKey(string typePrefix)
    {
        return string.IsNullOrWhiteSpace(typePrefix) ? SyncType : $"{typePrefix}-{SyncType}";
    }

    private async Task<long> GetSyncHeightAsync(string chainId, string typePrefix)
    {
        var settingKey = GetSettingKey(typePrefix);
        var setting = await SettingManager.GetOrNullAsync(chainId, settingKey);
        return setting == null ? 0 : long.Parse(setting);
    }
    private async Task SetSyncHeightAsync(string chainId, string typePrefix, long height)
    {
        var settingKey = GetSettingKey(typePrefix);
        await SettingManager.SetAsync(chainId, settingKey, height.ToString());
    }
    
    protected async Task<FilterLogsDto> GetContractLogsAsync(string chainId, string contractAddress, long startHeight, long endHeight)
    {
        var logs = await BlockchainAppService.GetContractLogsAsync(chainId, contractAddress, startHeight, endHeight);
        return logs;
    }

    protected abstract string SyncType { get; }

    protected abstract Task<long> HandleDataAsync(string chainId, long startHeight, long endHeight);
}