namespace FoodDiary.Presentation.Api.Features.Users.Responses;

public sealed record WeightGoalHistoryHttpResponse(
    Guid Id,
    double TargetWeightKg,
    double StartWeightKg,
    double? EndWeightKg,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);
