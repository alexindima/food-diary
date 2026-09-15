namespace FoodDiary.Modules.Identity.Contracts.Authentication.Common;

public sealed record EmailTemplateContent(string Subject, string HtmlBody, string TextBody);
