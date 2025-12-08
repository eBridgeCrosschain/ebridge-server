using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.CrossChainServer.Worker;

public class TransferAutoReceiveWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;

    public TransferAutoReceiveWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ICrossChainTransferAppService crossChainTransferAppService,
        IOptionsSnapshot<WorkerSyncPeriodOptions> workerSyncPeriodOptions) : base(timer,
        serviceScopeFactory)
    {
        Timer.Period = workerSyncPeriodOptions.Value.AutoReceiveSyncPeriod;
        _crossChainTransferAppService = crossChainTransferAppService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _crossChainTransferAppService.UpdateReceiveTransactionAsync();
        await _crossChainTransferAppService.AutoReceiveAsync();
    }
}