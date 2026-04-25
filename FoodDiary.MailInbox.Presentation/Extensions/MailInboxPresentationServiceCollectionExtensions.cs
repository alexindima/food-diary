using FoodDiary.MailInbox.Presentation.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.MailInbox.Presentation.Extensions;

public static class MailInboxPresentationServiceCollectionExtensions {
    public static IServiceCollection AddMailInboxPresentation(this IServiceCollection services) {
        services
            .AddControllers()
            .ConfigureApiBehaviorOptions(options => {
                options.InvalidModelStateResponseFactory = context => {
                    var errors = context.ModelState
                        .Where(static entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(
                            static entry => MailInboxApiErrorDetailsMapper.ToCamelCasePath(
                                string.IsNullOrWhiteSpace(entry.Key) ? "request" : entry.Key),
                            static entry => entry.Value!.Errors
                                .Select(static error => error.ErrorMessage)
                                .Where(static message => !string.IsNullOrWhiteSpace(message))
                                .DefaultIfEmpty("The value is invalid.")
                                .ToArray(),
                            StringComparer.Ordinal);

                    return new BadRequestObjectResult(new MailInboxApiErrorHttpResponse(
                        "Validation.Invalid",
                        "One or more validation errors occurred.",
                        context.HttpContext.TraceIdentifier,
                        errors.Count > 0 ? errors : null));
                };
            })
            .AddApplicationPart(typeof(MailInboxPresentationServiceCollectionExtensions).Assembly);
        return services;
    }
}
