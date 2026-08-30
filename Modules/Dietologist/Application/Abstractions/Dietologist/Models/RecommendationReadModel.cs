namespace FoodDiary.Application.Abstractions.Dietologist.Models;

public sealed record RecommendationReadModel(
    Guid RecommendationId,
    Guid DietologistUserId,
    string? DietologistFirstName,
    string? DietologistLastName,
    string Text,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);
