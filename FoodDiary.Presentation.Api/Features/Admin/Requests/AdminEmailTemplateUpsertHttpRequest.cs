namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record AdminEmailTemplateUpsertHttpRequest(
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive);
