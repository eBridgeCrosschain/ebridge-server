using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace AElf.CrossChainServer.ExceptionHandler;

public static class ExceptionHandlingService
{
    public static async Task<FlowBehavior> HandleException(Exception ex)
    {
        Log.Error("Handled exception: {message}",ex.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
    
    public static async Task<FlowBehavior> HandleExceptionReturnLong(Exception ex)
    {
        Log.Error("Handled exception: {message}",ex.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = 0
        };
    }
}