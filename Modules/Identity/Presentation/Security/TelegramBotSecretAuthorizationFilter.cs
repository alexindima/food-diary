using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Presentation.Api.Security;
using System.Diagnostics;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Presentation.Options;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Presentation.Security;

public sealed class TelegramBotSecretAuthorizationFilter(
    IOptions<TelegramBotAuthOptions> telegramBotOptions,
    ILogger<TelegramBotSecretAuthorizationFilter> logger)
    : IAsyncAuthorizationFilter {
    public const string SecretHeaderName = "X-Telegram-Bot-Secret";

    private readonly TelegramBotAuthOptions _telegramBotOptions = telegramBotOptions.Value;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context) {
        // ReSharper disable once ExplicitCallerInfoArgument
        using Activity? activity = PresentationApiTelemetry.ActivitySource.StartActivity("auth.telegram.bot-secret");
        activity?.SetTag("fooddiary.presentation.feature", "Auth");
        activity?.SetTag("fooddiary.presentation.controller", "AuthTelegramController");
        activity?.SetTag("fooddiary.presentation.operation", "auth.telegram.bot-secret");

        if (string.IsNullOrWhiteSpace(_telegramBotOptions.ApiSecret)) {
            TrackFailure(activity, "Authentication.TelegramBotNotConfigured");
            context.Result = CreateErrorResult(
                context,
                IdentityErrors.TelegramBotNotConfigured);
            return Task.CompletedTask;
        }

        string providedSecret = context.HttpContext.Request.Headers[SecretHeaderName].ToString();
        if (SecretComparison.FixedTimeEquals(_telegramBotOptions.ApiSecret, providedSecret)) {
            activity?.SetStatus(ActivityStatusCode.Ok);
            PresentationApiTelemetry.SecurityDecisionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.presentation.security.operation", "auth.telegram.bot-secret"),
                new KeyValuePair<string, object?>("fooddiary.presentation.security.outcome", "success"));
            logger.LogInformation("Telegram bot secret authorization succeeded");
            return Task.CompletedTask;
        }

        TrackFailure(activity, "Authentication.TelegramBotInvalidSecret");
        context.Result = CreateErrorResult(
            context,
            IdentityErrors.TelegramBotInvalidSecret);
        return Task.CompletedTask;
    }

    private static ObjectResult CreateErrorResult(
        AuthorizationFilterContext context,
        Error error) =>
        new(PresentationErrorHttpMapper.MapResponse(error, context.HttpContext.TraceIdentifier)) {
            StatusCode = PresentationErrorHttpMapper.MapStatusCode(error),
        };

    private void TrackFailure(Activity? activity, string errorCode) {
        activity?.SetStatus(ActivityStatusCode.Error, errorCode);
        activity?.SetTag("error.type", errorCode);
        PresentationApiTelemetry.SecurityDecisionCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.presentation.security.operation", "auth.telegram.bot-secret"),
            new KeyValuePair<string, object?>("fooddiary.presentation.security.outcome", "failure"),
            new KeyValuePair<string, object?>("error.code", errorCode));
        logger.LogWarning("Telegram bot secret authorization failed with {ErrorCode}", errorCode);
    }
}
