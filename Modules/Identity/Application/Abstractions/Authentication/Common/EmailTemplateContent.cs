namespace FoodDiary.Application.Abstractions.Authentication.Common;

public sealed record EmailTemplateContent(string Subject, string HtmlBody, string TextBody);
