using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace AElf.CrossChainServer.Chains;

public partial class AElfClientProvider
{
    private async Task<FlowBehavior> HandleGetTransactionResultException(Exception ex, string chainId, string transactionId)
    {
        Log.ForContext("chainId", chainId).Error(ex,
            "Get transaction result failed, ChainId: {key}, TransactionId: {transactionId}", chainId, transactionId);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw
        };
    }

}