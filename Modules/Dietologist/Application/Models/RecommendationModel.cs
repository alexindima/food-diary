namespace FoodDiary.Modules.Dietologist.Application.Models;

public sealed record RecommendationModel(
    Guid Id,
    Guid DietologistUserId,
    string? DietologistFirstName,
    string? DietologistLastName,
    string Text,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);
