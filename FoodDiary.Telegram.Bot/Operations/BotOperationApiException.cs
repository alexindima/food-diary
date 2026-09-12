using System.Net;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed class BotOperationApiException(string errorCode, HttpStatusCode statusCode)
    : HttpRequestException("Telegram operation API rejected the request.", inner: null, statusCode) {
    internal string ErrorCode { get; } = errorCode;
}
