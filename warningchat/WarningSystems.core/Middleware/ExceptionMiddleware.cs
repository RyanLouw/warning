using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace WarningSystems.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        _logger.Error(
            exception,
            "An unhandled exception occurred while processing {RequestMethod} {RequestPath}.",
            context.Request.Method,
            context.Request.Path);

        if (context.Response.HasStarted)
        {
            throw exception;
        }

        const int statusCode =
            StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = "An unexpected error occurred.",
            Detail = _environment.IsDevelopment()
                ? exception.Message
                : "The server was unable to complete the request.",
            Type = "https://httpstatuses.com/500",
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType =
            "application/problem+json";

        await context.Response.WriteAsJsonAsync(
            problemDetails);
    }
}