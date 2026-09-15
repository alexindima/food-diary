namespace FoodDiary.Modules.Identity.Contracts.Admin.Models;

public sealed record EmailTemplateRevisionReadModel(Guid Id, string Subject, string HtmlBody, string TextBody,
    bool IsActive, DateTime SavedOnUtc, DateTime ArchivedOnUtc);
