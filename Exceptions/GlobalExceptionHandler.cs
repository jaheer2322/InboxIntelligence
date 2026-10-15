using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InboxIntelligence.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statusCode, title, detail) = exception switch
            {
                BadRequestException ex => (
                    StatusCodes.Status400BadRequest,
                    "Invalid request",
                    ex.Message
                ),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error",
                    "Please try again later."
                )
            };

            if (statusCode == StatusCodes.Status500InternalServerError) {
                _logger.LogError(exception, title);
            }
            else
            {
                _logger.LogError(exception, "Request rejected", exception.Message);
            }

            httpContext.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            
            return true;
        }
    }
}
