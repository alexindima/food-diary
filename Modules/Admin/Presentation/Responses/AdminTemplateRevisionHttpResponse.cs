namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminTemplateRevisionHttpResponse(Guid Id, string? Subject, string? HtmlBody, string TextBody,
    bool IsActive, int? Version, DateTime SavedOnUtc, DateTime ArchivedOnUtc);
