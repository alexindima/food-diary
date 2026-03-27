using FoodDiary.Application.Authentication.Common;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Presentation.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class PresentationServiceCollectionExtensions {
    public static IServiceCollection AddPresentationApi(this IServiceCollection services) {
        services
            .AddControllers()
            .ConfigureApiBehaviorOptions(options => {
                options.InvalidModelStateResponseFactory = context => {
                    var errors = context.ModelState
                        .Where(static entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(
                            static entry => ApiErrorDetailsMapper.ToCamelCasePath(string.IsNullOrWhiteSpace(entry.Key) ? "request" : entry.Key),
                            static entry => entry.Value!.Errors
                                .Select(static error => error.ErrorMessage)
                                .Where(static message => !string.IsNullOrWhiteSpace(message))
                                .DefaultIfEmpty("The value is invalid.")
                                .ToArray(),
                            StringComparer.Ordinal);

                    return new BadRequestObjectResult(new ApiErrorHttpResponse(
                        "Validation.Invalid",
                        "One or more validation errors occurred.",
                        context.HttpContext.TraceIdentifier,
                        errors.Count > 0 ? errors : null));
                };
            });
        services.AddSignalR();
        services.AddScoped<TelegramBotSecretAuthorizationFilter>();
        services.AddSingleton<IUserIdProvider, UserIdProvider>();
        services.AddScoped<IEmailVerificationNotifier, EmailVerificationNotifier>();
        return services;
    }
}
