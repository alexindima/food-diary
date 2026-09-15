namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Models;

public sealed record RecommendationReadModel(
    Guid RecommendationId,
    Guid DietologistUserId,
    string? DietologistFirstName,
    string? DietologistLastName,
    string Text,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);
