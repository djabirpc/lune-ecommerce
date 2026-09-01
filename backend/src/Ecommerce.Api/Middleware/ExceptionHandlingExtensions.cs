using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Ecommerce.Api.Middleware;

public static class ExceptionHandlingExtensions
{
    public static IApplicationBuilder UseAppExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(builder =>
        {
            builder.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = feature?.Error;

                var (statusCode, response) = exception switch
                {
                    AppException appException => (appException.StatusCode, ErrorResponse.Create(appException.Code, appException.Message)),
                    ValidationException validationException => (400, ErrorResponse.Create(
                        "VALIDATION_ERROR",
                        string.Join(" ", validationException.Errors.Select(e => e.ErrorMessage)))),
                    _ => (500, ErrorResponse.Create("INTERNAL_ERROR", "Une erreur inattendue est survenue.")),
                };

                if (statusCode == 500)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);
                }

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(response);
            });
        });
    }
}
