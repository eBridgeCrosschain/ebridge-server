using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.CrossChainServer.Worker.IndexerSync;

public class IndexerSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IChainAppService _chainAppService;
    private readonly IEnumerable<IIndexerSyncProvider> _indexerSyncProviders;
    private readonly BridgeContractSyncOptions _bridgeContractSyncOptions;
    
    public IndexerSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IEnumerable<IIndexerSyncProvider> indexerSyncProviders, IChainAppService chainAppService,
        IOptionsSnapshot<BridgeContractSyncOptions> bridgeContractSyncOptions) : base(timer,
        serviceScopeFactory)
    {
        _bridgeContractSyncOptions = bridgeContractSyncOptions.Value;
        _chainAppService = chainAppService;
        _indexerSyncProviders = indexerSyncProviders.ToList();
        Timer.Period = 1000 * 1;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var chains = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.AElf
        });

        Log.Debug("Start to sync chain.");
        var tasks = 
            chains.Items.Select(o => o.Id).SelectMany(chainId =>
            _indexerSyncProviders.Select(async provider => await provider.ExecuteAsync(chainId, _bridgeContractSyncOptions.SyncDelayHeight)));
        
        await Task.WhenAll(tasks);

        var confirmTasks = chains.Items.Select(o => o.Id).SelectMany(chainId =>
            _indexerSyncProviders.Select(async provider => await provider.ExecuteAsync(chainId,
                _bridgeContractSyncOptions.ConfirmedSyncDelayHeight,
                _bridgeContractSyncOptions.ConfirmedSyncKeyPrefix)));

        await Task.WhenAll(confirmTasks);
    }
}