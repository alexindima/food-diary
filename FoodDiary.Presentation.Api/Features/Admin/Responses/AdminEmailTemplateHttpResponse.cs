namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminEmailTemplateHttpResponse(
    Guid Id,
    string Key,
    string Locale,
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
