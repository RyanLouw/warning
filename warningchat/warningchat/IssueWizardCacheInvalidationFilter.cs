using Microsoft.AspNetCore.Mvc.Filters;
using WarningSystems.Core.Caching;

namespace WarningSystems;

// Invalidate after the whole action (including explicit DB transactions) finishes.
// Also invalidate on errors: an action may have persisted part of its work.
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
                // Issue saves do not change the employee picker, categories or SOPs.
                var controller = context.ActionDescriptor.RouteValues["controller"];
                cache.Invalidate(includeReferenceData:
                    !string.Equals(controller, "Transgression", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
