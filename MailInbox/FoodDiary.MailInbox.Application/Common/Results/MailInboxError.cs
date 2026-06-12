namespace FoodDiary.MailInbox.Application.Common.Results;

public sealed record MailInboxError(
    string Code,
    string Message,
    ErrorKind Kind = ErrorKind.Internal,
    IReadOnlyDictionary<string, string[]>? Details = null);
