using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace AElf.CrossChainServer.CrossChain;

public partial class CrossChainTransferAppService
{
    public async Task<FlowBehavior> HandleDbException(Exception ex, CrossChainTransfer input)
    {
        Log.Error($"DbUpdateConcurrencyException: {ex.Message}");
        await HandleUniqueTransfer(ex, input);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
    
    public async Task<FlowBehavior> HandleAutoReceiveException(Exception ex, CrossChainTransfer transfer,List<CrossChainTransfer> toUpdate)
    {
        Log.ForContext("fromChainId",transfer.FromChainId).ForContext("toChainId",transfer.ToChainId).Error($"Send auto receive failed: {ex.Message}");
        await AddReceiveTransactionAttemptTimes(transfer, toUpdate);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
}