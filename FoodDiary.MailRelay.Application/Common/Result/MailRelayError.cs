namespace FoodDiary.MailRelay.Application.Common.Result;

public sealed record MailRelayError(
    string Code,
    string Message,
    ErrorKind Kind = ErrorKind.Internal,
    IReadOnlyDictionary<string, string[]>? Details = null);
