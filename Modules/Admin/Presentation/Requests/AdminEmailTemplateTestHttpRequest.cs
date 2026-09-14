namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminEmailTemplateTestHttpRequest(
    string ToEmail,
    string Key,
    string Subject,
    string HtmlBody,
    string TextBody);
