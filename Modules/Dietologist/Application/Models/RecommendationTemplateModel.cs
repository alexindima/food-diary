namespace FoodDiary.Modules.Dietologist.Application.Models;

public sealed record RecommendationTemplateModel(
    Guid Id,
    string Name,
    string Text,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc);
