using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblemAsync(context, ex, (int)HttpStatusCode.Unauthorized, "Unauthorized");
        }
        catch (KeyNotFoundException ex)
        {
            await WriteProblemAsync(context, ex, (int)HttpStatusCode.NotFound, "Not Found");
        }
        catch (ArgumentException ex)
        {
            await WriteProblemAsync(context, ex, (int)HttpStatusCode.BadRequest, "Bad Request");
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblemAsync(context, ex, (int)HttpStatusCode.BadRequest, "Bad Request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            await WriteProblemAsync(context, ex,
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                includeDetails: false); 
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        Exception ex,
        int statusCode,
        string title,
        bool includeDetails = true)
    {
        if (context.Response.HasStarted)
        {
            throw ex;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = includeDetails ? ex.Message : null,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problem);
        await context.Response.WriteAsync(json);
    }
}