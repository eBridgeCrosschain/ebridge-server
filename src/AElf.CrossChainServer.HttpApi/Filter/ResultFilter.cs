using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AElf.CrossChainServer.Filter;

public class ResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is EmptyResult || context.Result is NoContentResult)
        {
            context.HttpContext.Response.StatusCode = 200;
            context.Result = new ObjectResult(new ResponseDto().NoContent());
        }
        else if (context.Result is ObjectResult objectResult)
        {
            if (!(objectResult.Value is ResponseDto))
            {
                context.Result = new ObjectResult(new ResponseDto().ObjectResult(objectResult.Value));
            }
        }
        else if (context.Result is StatusCodeResult statusCodeResult)
        {
            context.Result =
                new ObjectResult(new ResponseDto().StatusCodeResult(context.HttpContext.Response.StatusCode,
                    context.Result.GetType().Name));
            context.HttpContext.Response.StatusCode = 200;
        }

        await next();
    }
}