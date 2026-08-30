namespace FoodDiary.Application.Dietologist.Models;

public sealed record RecommendationTemplateModel(
    Guid Id,
    string Name,
    string Text,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc);
