namespace FoodDiary.Presentation.Api.Features.Users.Responses;

public sealed record WaistGoalHistoryHttpResponse(
    Guid Id,
    double TargetWaistCm,
    double StartWaistCm,
    double? EndWaistCm,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);
