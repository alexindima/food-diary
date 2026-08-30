namespace FoodDiary.Application.Abstractions.Dietologist.Models;

public sealed record RecommendationTemplateReadModel(
    Guid Id,
    string Name,
    string Text,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc);
