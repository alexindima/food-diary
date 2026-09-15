namespace FoodDiary.Modules.Users.Presentation.Contracts.Responses;

public sealed record WaistGoalHistoryHttpResponse(
    Guid Id,
    double TargetWaistCm,
    double StartWaistCm,
    double? EndWaistCm,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);
