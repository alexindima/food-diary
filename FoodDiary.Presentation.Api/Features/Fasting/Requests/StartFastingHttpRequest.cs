namespace FoodDiary.Presentation.Api.Features.Fasting.Requests;

public sealed record StartFastingHttpRequest(
    string? Protocol = null,
    string? PlanType = null,
    int? PlannedDurationHours = null,
    int? CyclicFastDays = null,
    int? CyclicEatDays = null,
    int? CyclicEatDayFastHours = null,
    int? CyclicEatDayEatingWindowHours = null,
    string? Notes = null);
