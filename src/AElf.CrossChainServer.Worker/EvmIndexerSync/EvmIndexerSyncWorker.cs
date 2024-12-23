using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Worker.IndexerSync;
using AElf.CrossChainServer.Worker.TokenPoolSync;
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
    
    public EvmIndexerSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IEnumerable<IEvmSyncProvider> evmSyncProviders, IChainAppService chainAppService) : base(
        timer,
        serviceScopeFactory)
    {
        _evmSyncProviders = evmSyncProviders.ToList();
        _chainAppService = chainAppService;
        Timer.Period = 1000 * 60;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var chains = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.Evm
        });

        Log.Debug("Start to sync evm chain.");
        var tasks = 
            chains.Items.Select(o => o.Id).SelectMany(chainId =>
                _evmSyncProviders.Select(async provider => await provider.ExecuteAsync(chainId)));
        
        await Task.WhenAll(tasks);
    }
}