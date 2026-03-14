using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace PromoEngine.Api.Extensions;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, IHostEnvironment hostEnvironment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = exception is ValidationException ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
        httpContext.Response.StatusCode = statusCode;

        var detail = statusCode == StatusCodes.Status400BadRequest
            ? exception.Message
            : hostEnvironment.IsDevelopment() || hostEnvironment.IsEnvironment("Testing")
                ? exception.ToString()
                : "An unexpected error occurred.";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status400BadRequest ? "Validation failed" : "Server error",
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
