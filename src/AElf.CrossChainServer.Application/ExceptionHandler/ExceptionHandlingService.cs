using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace AElf.CrossChainServer.ExceptionHandler;

public class ExceptionHandlingService
{
    public static async Task<FlowBehavior> HandleException(Exception ex)
    {
        Console.WriteLine($"Handled exception: {ex.Message}");
        await Task.Delay(100);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }
    
    public static async Task<FlowBehavior> HandleExceptionWithOutReturnValue(Exception ex)
    {
        Console.WriteLine($"Handled exception: {ex.Message}");
        await Task.Delay(100);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
}