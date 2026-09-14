namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminEmailTemplateUpsertHttpRequest(
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive);
