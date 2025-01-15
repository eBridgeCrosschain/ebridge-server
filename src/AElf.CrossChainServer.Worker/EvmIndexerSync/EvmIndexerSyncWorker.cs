using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public class EvmIndexerSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IChainAppService _chainAppService;
    private readonly IEnumerable<IEvmSyncProvider> _evmSyncProviders;
    private readonly EvmContractSyncOptions _evmContractSyncOptions;

    public EvmIndexerSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IEnumerable<IEvmSyncProvider> evmSyncProviders, IChainAppService chainAppService,IOptionsSnapshot<EvmContractSyncOptions> evmContractSyncOptions) : base(
        timer,
        serviceScopeFactory)
    {
        _evmSyncProviders = evmSyncProviders.ToList();
        _chainAppService = chainAppService;
        _evmContractSyncOptions = evmContractSyncOptions.Value;
        Timer.Period = _evmContractSyncOptions.SyncPeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        if(!_evmContractSyncOptions.Enabled)
        {
            Log.Debug("Evm sync is disabled.");
            return;
        }
        var chains = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.Evm
        });

        Log.Debug("Start to sync evm chain.");
        foreach (var chain in chains.Items)
        {
            foreach (var provider in _evmSyncProviders)
            {
                await provider.ExecuteAsync(chain.Id);
            }
        }
    }
}