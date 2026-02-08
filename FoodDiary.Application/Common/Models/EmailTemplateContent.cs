namespace FoodDiary.Application.Common.Models;

public sealed record EmailTemplateContent(
    string Subject,
    string HtmlBody,
    string TextBody);
