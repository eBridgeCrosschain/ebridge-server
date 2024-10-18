using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace AElf.CrossChainServer.CrossChain;

public partial class CrossChainTransferAppService
{
    public async Task<FlowBehavior> HandleTransferDbException(Exception ex, CrossChainTransfer transfer)
    {
        Log.Error($"DbUpdateConcurrencyException: {ex.Message}");
        await HandleUniqueTransfer(ex, transfer);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
}