using Microsoft.AspNetCore.Mvc.Filters;
using WarningSystems.Core.Caching;

namespace WarningSystems;

public sealed class IssueWizardCacheInvalidationFilter(IssueWizardCache cache) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var action = context.ActionDescriptor.RouteValues["action"];
        var mutates = !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)
            || string.Equals(action, "DeleteAttachment", StringComparison.OrdinalIgnoreCase);
        try
        {
            await next();
        }
        finally
        {
            if (mutates)
            {
                var controller = context.ActionDescriptor.RouteValues["controller"];
                cache.Invalidate(includeReferenceData:
                    string.Equals(controller, "Admin", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
