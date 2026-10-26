using App.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace App.Api.Middleware
{
    /// <summary>
    /// Converts unhandled application exceptions into RFC 7807 problem details responses.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the request pipeline.</param>
        /// <param name="logger">Logger used to record unexpected failures.</param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Executes the middleware and writes an error response if the pipeline throws.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
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

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, errors) = exception switch
            {
                ValidationException validationException => (StatusCodes.Status400BadRequest,
                    validationException.Errors.Select(f => f.ErrorMessage).ToList()),
                NotFoundException => (StatusCodes.Status404NotFound, [exception.Message]),
                ConflictException => (StatusCodes.Status409Conflict, [exception.Message]),
                ForbiddenException => (StatusCodes.Status403Forbidden, [exception.Message]),
                InvalidRoleException => (StatusCodes.Status403Forbidden, [exception.Message]),
                HttpRequestException => (StatusCodes.Status400BadRequest, [exception.Message]),
                _ => (StatusCodes.Status500InternalServerError, ["An unexpected error occurred."])
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Unhandled exception.");

            context.Response.StatusCode = statusCode;
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = ReasonPhrases.GetReasonPhrase(statusCode),
                Extensions = { ["errors"] = errors }
            };
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
