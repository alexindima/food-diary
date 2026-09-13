namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminTemplateRevisionModel(Guid Id, string? Subject, string? HtmlBody, string TextBody,
    bool IsActive, int? Version, DateTime SavedOnUtc, DateTime ArchivedOnUtc);
