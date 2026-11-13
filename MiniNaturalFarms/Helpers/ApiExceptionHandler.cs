using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Common;

namespace MiniNaturalFarms.Helpers;

public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;
    private readonly bool _showDetails;

    public ApiExceptionHandler(
        ILogger<ApiExceptionHandler> logger,
        IConfiguration config,
        IHostEnvironment env)
    {
        _logger = logger;
        // Show full error details in Development, or when the Errors:ShowDetails setting is true
        _showDetails = env.IsDevelopment() || config.GetValue<bool>("Errors:ShowDetails");
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ExternalServiceException => (StatusCodes.Status502BadGateway, "Payment service error"),
            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }

        var detail = status == StatusCodes.Status500InternalServerError
            ? (_showDetails ? ex.ToString() : "Something went wrong.")
            : ex.Message;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        }, ct);

        return true;
    }
}
