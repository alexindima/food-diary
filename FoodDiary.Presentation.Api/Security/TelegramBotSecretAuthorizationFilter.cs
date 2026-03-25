using FoodDiary.Presentation.Api.Options;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace FoodDiary.Presentation.Api.Security;

public sealed class TelegramBotSecretAuthorizationFilter(IOptions<TelegramBotAuthOptions> telegramBotOptions)
    : IAsyncAuthorizationFilter {
    public const string SecretHeaderName = "X-Telegram-Bot-Secret";

    private readonly TelegramBotAuthOptions _telegramBotOptions = telegramBotOptions.Value;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context) {
        if (string.IsNullOrWhiteSpace(_telegramBotOptions.ApiSecret)) {
            context.Result = CreateErrorResult(
                context,
                StatusCodes.Status500InternalServerError,
                "Authentication.TelegramBotNotConfigured",
                "Telegram bot authentication is not configured.");
            return Task.CompletedTask;
        }

        var providedSecret = context.HttpContext.Request.Headers[SecretHeaderName].ToString();
        if (SecretComparison.FixedTimeEquals(_telegramBotOptions.ApiSecret, providedSecret)) {
            return Task.CompletedTask;
        }

        context.Result = CreateErrorResult(
            context,
            StatusCodes.Status401Unauthorized,
            "Authentication.TelegramBotInvalidSecret",
            "Telegram bot secret is invalid.");
        return Task.CompletedTask;
    }

    private static ObjectResult CreateErrorResult(
        AuthorizationFilterContext context,
        int statusCode,
        string errorCode,
        string message) =>
        new(new ApiErrorHttpResponse(errorCode, message, context.HttpContext.TraceIdentifier)) {
            StatusCode = statusCode,
        };
}
