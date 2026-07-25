using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SeatFlow.Application.Authentication;
using SeatFlow.Application.Common;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.Api.Errors;

public sealed class ApiExceptionHandler
    : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(
        ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var error = MapException(exception);

        if (error.StatusCode >=
            StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred.");
        }
        else
        {
            _logger.LogWarning(
                exception,
                "A request failed with status code {StatusCode}.",
                error.StatusCode);
        }

        var problemDetails =
            new ProblemDetails
            {
                Status = error.StatusCode,
                Title = error.Title,
                Detail = error.Detail,
                Instance =
                    httpContext.Request.Path
            };

        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id ??
            httpContext.TraceIdentifier;

        httpContext.Response.StatusCode =
            error.StatusCode;

        httpContext.Response.ContentType =
            "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken:
                cancellationToken);

        return true;
    }

    private static ApiError MapException(
        Exception exception)
    {
        return exception switch
        {
            DomainValidationException =>
                new ApiError(
                    StatusCodes.Status400BadRequest,
                    "Validation error",
                    exception.Message),

            ResourceNotFoundException =>
                new ApiError(
                    StatusCodes.Status404NotFound,
                    "Resource not found",
                    exception.Message),

            InvalidCredentialsException =>
                new ApiError(
                    StatusCodes.Status401Unauthorized,
                    "Authentication failed",
                    exception.Message),

            InvalidRefreshTokenException =>
                new ApiError(
                    StatusCodes.Status401Unauthorized,
                    "Invalid refresh token",
                    exception.Message),

            InactiveUserException =>
                new ApiError(
                    StatusCodes.Status403Forbidden,
                    "User is inactive",
                    exception.Message),

            UserAlreadyExistsException =>
                new ApiError(
                    StatusCodes.Status409Conflict,
                    "User already exists",
                    exception.Message),

            DomainConflictException =>
                new ApiError(
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    exception.Message),

            _ =>
                new ApiError(
                    StatusCodes.Status500InternalServerError,
                    "Internal server error",
                    "An unexpected error occurred.")
        };
    }

    private sealed record ApiError(
        int StatusCode,
        string Title,
        string Detail);
}