namespace FoodDiary.Contracts.Admin;

public sealed record AdminEmailTemplateUpsertRequest(
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive);
