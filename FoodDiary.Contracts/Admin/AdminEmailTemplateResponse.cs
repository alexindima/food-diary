namespace FoodDiary.Contracts.Admin;

public sealed record AdminEmailTemplateResponse(
    Guid Id,
    string Key,
    string Locale,
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
