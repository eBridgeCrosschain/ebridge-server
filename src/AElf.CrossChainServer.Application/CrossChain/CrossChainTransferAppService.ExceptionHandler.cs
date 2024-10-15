using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace AElf.CrossChainServer.CrossChain;

public partial class CrossChainTransferAppService
{
    public async Task<FlowBehavior> HandleDbException<T>(Exception ex, T input)
    {
        Log.Error($"DbUpdateConcurrencyException: {ex.Message}");
        await HandleUniqueTransfer(ex, input);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
}