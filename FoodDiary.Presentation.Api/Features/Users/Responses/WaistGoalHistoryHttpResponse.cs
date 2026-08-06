namespace FoodDiary.Presentation.Api.Features.Users.Responses;

public sealed record WaistGoalHistoryHttpResponse(
    Guid Id,
    double TargetWaist,
    double StartWaist,
    double? EndWaist,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);
