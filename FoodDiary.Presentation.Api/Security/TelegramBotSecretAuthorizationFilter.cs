using FoodDiary.Presentation.Api.Options;
using FoodDiary.Presentation.Api.Responses;
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
                FoodDiary.Application.Common.Abstractions.Result.Errors.Authentication.TelegramBotNotConfigured);
            return Task.CompletedTask;
        }

        var providedSecret = context.HttpContext.Request.Headers[SecretHeaderName].ToString();
        if (SecretComparison.FixedTimeEquals(_telegramBotOptions.ApiSecret, providedSecret)) {
            return Task.CompletedTask;
        }

        context.Result = CreateErrorResult(
            context,
            FoodDiary.Application.Common.Abstractions.Result.Errors.Authentication.TelegramBotInvalidSecret);
        return Task.CompletedTask;
    }

    private static ObjectResult CreateErrorResult(
        AuthorizationFilterContext context,
        FoodDiary.Application.Common.Abstractions.Result.Error error) =>
        new(new ApiErrorHttpResponse(
            error.Code,
            error.Message,
            context.HttpContext.TraceIdentifier,
            ApiErrorDetailsMapper.Normalize(error.Details))) {
            StatusCode = PresentationErrorHttpMapper.MapStatusCode(error),
        };
}
