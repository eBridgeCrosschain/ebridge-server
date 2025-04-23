using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.CrossChainServer.Worker;

public class CheckEvmTransactionConfirmedWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly IChainAppService _chainAppService;

    public CheckEvmTransactionConfirmedWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ICrossChainTransferAppService crossChainTransferAppService, IChainAppService chainAppService) : base(timer,
        serviceScopeFactory)
    {
        Timer.Period = 1000 * 60;
        _crossChainTransferAppService = crossChainTransferAppService;
        _chainAppService = chainAppService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var chainList = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.Evm
        });
        Log.Debug("Start to check evm confirmed transfer transaction.");
        var tasks =
            chainList.Items.Select(o => o.Id).Select(async chainId =>
            {
                await _crossChainTransferAppService.CheckEvmTransferTransactionConfirmedAsync(chainId);
            });

        await Task.WhenAll(tasks);
        Log.Debug("Start to check evm confirmed receive transaction.");
        var tasksReceive =
            chainList.Items.Select(o => o.Id).Select(async chainId =>
            {
                await _crossChainTransferAppService.CheckEvmReceiveTransactionConfirmedAsync(chainId);
            });

        await Task.WhenAll(tasksReceive);
    }
}