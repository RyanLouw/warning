using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using WarningSystems.Core.Caching;

namespace WarningSystems;

public sealed class IssueWizardCacheInvalidationFilter(
    IssueWizardCache cache) : IAsyncActionFilter
{
    private static readonly HashSet<string> ReferenceActions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CreateQuestion",
            "EditQuestion",
            "CreateCategory",
            "EditCategory",
            "SetCategoryQuestionLink"
        };

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var action = context.ActionDescriptor.RouteValues["action"];
        var controller = context.ActionDescriptor.RouteValues["controller"];

        var mutates =
            (!HttpMethods.IsGet(request.Method) &&
             !HttpMethods.IsHead(request.Method)) ||
            string.Equals(
                action,
                "DeleteAttachment",
                StringComparison.OrdinalIgnoreCase);

        var warningIds = new HashSet<long>();

        if (mutates)
            CollectWarningIds(context, warningIds);

        try
        {
            await next();
        }
        finally
        {
            if (mutates)
            {
                CollectWarningIds(context, warningIds);

                foreach (var warningId in warningIds)
                    cache.InvalidateWarning(warningId);

                if (string.Equals(
                        controller,
                        "Admin",
                        StringComparison.OrdinalIgnoreCase) &&
                    action is not null &&
                    ReferenceActions.Contains(action))
                {
                    cache.InvalidateReferenceData();
                }
            }
        }
    }

    private static void CollectWarningIds(
        ActionExecutingContext context,
        HashSet<long> ids)
    {
        foreach (var argument in context.ActionArguments)
        {
            if (string.Equals(
                    argument.Key,
                    "warningId",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddId(argument.Value, ids);
            }
            else
            {
                CollectFromModel(argument.Value, ids);
            }
        }
    }

    private static void CollectFromModel(
        object? model,
        HashSet<long> ids)
    {
        if (model is null || model is string)
            return;

        if (model is IEnumerable items)
        {
            foreach (var item in items)
                CollectFromModel(item, ids);

            return;
        }

        var property = model.GetType().GetProperty("WarningId");

        if (property?.CanRead == true)
            AddId(property.GetValue(model), ids);
    }

    private static void AddId(object? value, HashSet<long> ids)
    {
        if (value is long longId && longId > 0)
            ids.Add(longId);
        else if (value is int intId && intId > 0)
            ids.Add(intId);
    }
}