namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

public sealed record AdminEmailTemplateTestHttpRequest(
    string ToEmail,
    string Key,
    string Subject,
    string HtmlBody,
    string TextBody);
