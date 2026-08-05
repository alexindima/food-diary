namespace FoodDiary.Presentation.Api.Features.Users.Responses;

public sealed record WeightGoalHistoryHttpResponse(
    Guid Id,
    double TargetWeight,
    double StartWeight,
    double? EndWeight,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);
