using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace AElf.CrossChainServer.ExceptionHandler;

public static class ExceptionHandlingService
{
    public static async Task<FlowBehavior> HandleException(Exception ex, string message)
    {
        Log.Error(ex,$"Handled exception: {message}",ex.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }
    
    
    public static async Task<FlowBehavior> HandleExceptionWithOutReturnValue(Exception ex)
    {
        // Log.Error(ex,$"Handled exception: {message}",ex.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
    
    public static async Task<FlowBehavior> ThrowException(Exception ex, string message)
    {
        Log.Error(ex,$"Handled exception: {message}",ex.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = null
        };
    }
    
    public static async Task<FlowBehavior> HandleExceptionAndContinue(Exception ex, string message)
    {
        Log.Error(ex,$"Handled exception: {message}",ex.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue,
            ReturnValue = null
        };
    }
}