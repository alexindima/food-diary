using System.Diagnostics;
using FoodDiary.Presentation.Api.Options;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Presentation.Api.Security;

public sealed class TelegramBotSecretAuthorizationFilter(
    IOptions<TelegramBotAuthOptions> telegramBotOptions,
    ILogger<TelegramBotSecretAuthorizationFilter> logger)
    : IAsyncAuthorizationFilter {
    public const string SecretHeaderName = "X-Telegram-Bot-Secret";

    private readonly TelegramBotAuthOptions _telegramBotOptions = telegramBotOptions.Value;
    private readonly ILogger<TelegramBotSecretAuthorizationFilter> _logger = logger;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context) {
        using var activity = PresentationApiTelemetry.ActivitySource.StartActivity("auth.telegram.bot-secret", ActivityKind.Internal);
        activity?.SetTag("fooddiary.presentation.feature", "Auth");
        activity?.SetTag("fooddiary.presentation.controller", "AuthTelegramController");
        activity?.SetTag("fooddiary.presentation.operation", "auth.telegram.bot-secret");

        if (string.IsNullOrWhiteSpace(_telegramBotOptions.ApiSecret)) {
            TrackFailure(activity, "Authentication.TelegramBotNotConfigured");
            context.Result = CreateErrorResult(
                context,
                FoodDiary.Application.Abstractions.Common.Abstractions.Result.Errors.Authentication.TelegramBotNotConfigured);
            return Task.CompletedTask;
        }

        var providedSecret = context.HttpContext.Request.Headers[SecretHeaderName].ToString();
        if (SecretComparison.FixedTimeEquals(_telegramBotOptions.ApiSecret, providedSecret)) {
            activity?.SetStatus(ActivityStatusCode.Ok);
            PresentationApiTelemetry.OperationCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.presentation.feature", "Auth"),
                new KeyValuePair<string, object?>("fooddiary.presentation.controller", "AuthTelegramController"),
                new KeyValuePair<string, object?>("fooddiary.presentation.operation", "auth.telegram.bot-secret"),
                new KeyValuePair<string, object?>("fooddiary.presentation.outcome", "success"));
            _logger.LogInformation("Telegram bot secret authorization succeeded");
            return Task.CompletedTask;
        }

        TrackFailure(activity, "Authentication.TelegramBotInvalidSecret");
        context.Result = CreateErrorResult(
            context,
            FoodDiary.Application.Abstractions.Common.Abstractions.Result.Errors.Authentication.TelegramBotInvalidSecret);
        return Task.CompletedTask;
    }

    private static ObjectResult CreateErrorResult(
        AuthorizationFilterContext context,
        FoodDiary.Application.Abstractions.Common.Abstractions.Result.Error error) =>
        new(new ApiErrorHttpResponse(
            error.Code,
            error.Message,
            context.HttpContext.TraceIdentifier,
            ApiErrorDetailsMapper.Normalize(error.Details))) {
            StatusCode = PresentationErrorHttpMapper.MapStatusCode(error),
        };

    private void TrackFailure(Activity? activity, string errorCode) {
        activity?.SetStatus(ActivityStatusCode.Error, errorCode);
        activity?.SetTag("error.type", errorCode);
        PresentationApiTelemetry.OperationCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.presentation.feature", "Auth"),
            new KeyValuePair<string, object?>("fooddiary.presentation.controller", "AuthTelegramController"),
            new KeyValuePair<string, object?>("fooddiary.presentation.operation", "auth.telegram.bot-secret"),
            new KeyValuePair<string, object?>("fooddiary.presentation.outcome", "failure"));
        PresentationApiTelemetry.OperationFailureCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.presentation.feature", "Auth"),
            new KeyValuePair<string, object?>("fooddiary.presentation.controller", "AuthTelegramController"),
            new KeyValuePair<string, object?>("fooddiary.presentation.operation", "auth.telegram.bot-secret"),
            new KeyValuePair<string, object?>("error.code", errorCode));
        _logger.LogWarning("Telegram bot secret authorization failed with {ErrorCode}", errorCode);
    }
}
