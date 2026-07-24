namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record ClientTaskHttpResponse(
    Guid Id,
    Guid DietologistUserId,
    Guid ClientUserId,
    string Title,
    string? Details,
    DateTime? DueAtUtc,
    string Status,
    bool IsOverdue,
    DateTime CreatedAtUtc,
    DateTime? StatusChangedAtUtc);
