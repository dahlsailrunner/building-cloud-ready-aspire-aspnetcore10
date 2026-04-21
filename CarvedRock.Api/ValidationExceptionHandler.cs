using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRock.Api;

public sealed class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger)
                            : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation error",
            Detail = "One or more validation errors occurred."
        };

        foreach (var err in validationException.Errors)
        {
            if (string.IsNullOrWhiteSpace(err.PropertyName))
            {
                problemDetails.Extensions["Root-level"] = err.ErrorMessage;
                validationException.Data["Root-level"] = err.ErrorMessage; // only needed if logging
            }
            else
            {
                problemDetails.Extensions[err.PropertyName] = err.ErrorMessage;
                validationException.Data[err.PropertyName] = err.ErrorMessage; // only needed if logging
            }
        }

        // OPTIONAL!  If you don't want to log these, don't inject an ILogger
        logger.LogWarning(validationException, "Validation error.");

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
