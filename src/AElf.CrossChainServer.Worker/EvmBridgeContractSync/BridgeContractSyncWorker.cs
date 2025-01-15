using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.CrossChainServer.Worker;

public class BridgeContractSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IBridgeContractSyncService _bridgeContractSyncService;

    public BridgeContractSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IBridgeContractSyncService bridgeContractSyncService) : base(
        timer,
        serviceScopeFactory)
    {
        _bridgeContractSyncService = bridgeContractSyncService;
        Timer.Period = 1000 * 60;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _bridgeContractSyncService.ExecuteAsync();
    }
}