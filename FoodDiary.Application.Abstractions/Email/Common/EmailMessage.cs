namespace FoodDiary.Application.Abstractions.Email.Common;

public sealed record EmailMessage(
    string FromAddress,
    string FromName,
    IReadOnlyList<string> ToAddresses,
    string Subject,
    string HtmlBody,
    string? TextBody);
