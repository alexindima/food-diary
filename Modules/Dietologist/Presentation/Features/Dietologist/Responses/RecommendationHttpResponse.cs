namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record RecommendationHttpResponse(
    Guid Id,
    Guid DietologistUserId,
    string? DietologistFirstName,
    string? DietologistLastName,
    string Text,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);
