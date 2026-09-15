namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Models;

public sealed record RecommendationTemplateReadModel(
    Guid Id,
    string Name,
    string Text,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc);
