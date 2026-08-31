namespace FoodDiary.Application.Admin.Models;

public sealed record AdminEmailTemplateModel(
    Guid Id,
    string Key,
    string Locale,
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
