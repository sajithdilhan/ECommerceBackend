using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Shared.Filters;

public class RequestLoggingHandler(ILogger<RequestLoggingHandler> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var actionName = context.ActionDescriptor.DisplayName;
        logger.LogInformation("Starting execution of: {ActionName}", actionName);

        Stopwatch stopwatch = Stopwatch.StartNew();

        ActionExecutedContext resultContext = await next();

        stopwatch.Stop();
        if (resultContext.Exception == null)
        {
            logger.LogInformation("Completed: {ActionName} in {Elapsed}ms",
                actionName, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            logger.LogError("Failed: {ActionName} with error: {Error}",
                actionName, resultContext.Exception.Message);
        }
    }
}
