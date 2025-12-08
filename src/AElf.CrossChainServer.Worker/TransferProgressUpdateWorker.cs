using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.CrossChainServer.Worker;

public class TransferProgressUpdateWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;

    public TransferProgressUpdateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ICrossChainTransferAppService crossChainTransferAppService,
        IOptionsSnapshot<WorkerSyncPeriodOptions> workerSyncPeriodOptions) : base(timer, serviceScopeFactory)
    {
        _crossChainTransferAppService = crossChainTransferAppService;
        Timer.Period = workerSyncPeriodOptions.Value.ProgressUpdatePeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _crossChainTransferAppService.UpdateProgressAsync();
    }
}