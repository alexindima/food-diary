using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
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
                var fallbackFactory = options.InvalidModelStateResponseFactory;
                options.InvalidModelStateResponseFactory = context => {
                    if (context.HttpContext.Items.TryGetValue(CurrentUserIdModelBinder.UnauthorizedItemKey, out var unauthorized) &&
                        unauthorized is true) {
                        return new UnauthorizedResult();
                    }

                    var errors = context.ModelState.Values
                        .SelectMany(static entry => entry.Errors)
                        .Select(static error => error.ErrorMessage)
                        .Where(static message => !string.IsNullOrWhiteSpace(message))
                        .ToArray();

                    var message = errors.Length > 0
                        ? string.Join("; ", errors)
                        : "The request is invalid.";

                    return new BadRequestObjectResult(new ApiErrorHttpResponse(
                        "Validation.Invalid",
                        message,
                        context.HttpContext.TraceIdentifier));
                };
            });
        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, UserIdProvider>();
        services.AddScoped<IEmailVerificationNotifier, EmailVerificationNotifier>();
        return services;
    }
}
