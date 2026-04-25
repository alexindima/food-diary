using FoodDiary.MailRelay.Presentation.Filters;
using FoodDiary.MailRelay.Presentation.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.MailRelay.Presentation.Extensions;

public static class MailRelayPresentationServiceCollectionExtensions {
    public static IServiceCollection AddMailRelayPresentation(this IServiceCollection services) {
        services.AddScoped<MailRelayTelemetryActionFilter>();
        services.AddScoped<RelayApiKeyAuthorizationFilter>();
        services
            .AddControllers(options => {
                options.Filters.AddService<MailRelayTelemetryActionFilter>();
            })
            .ConfigureApiBehaviorOptions(options => {
                options.InvalidModelStateResponseFactory = context => {
                    var errors = context.ModelState
                        .Where(static entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(
                            static entry => MailRelayApiErrorDetailsMapper.ToCamelCasePath(
                                string.IsNullOrWhiteSpace(entry.Key) ? "request" : entry.Key),
                            static entry => entry.Value!.Errors
                                .Select(static error => error.ErrorMessage)
                                .Where(static message => !string.IsNullOrWhiteSpace(message))
                                .DefaultIfEmpty("The value is invalid.")
                                .ToArray(),
                            StringComparer.Ordinal);

                    return new BadRequestObjectResult(new MailRelayApiErrorHttpResponse(
                        "Validation.Invalid",
                        "One or more validation errors occurred.",
                        context.HttpContext.TraceIdentifier,
                        errors.Count > 0 ? errors : null));
                };
            })
            .AddApplicationPart(typeof(MailRelayPresentationServiceCollectionExtensions).Assembly);

        return services;
    }
}
