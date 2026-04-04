namespace FoodDiary.Presentation.Api.Features.Fasting.Requests;

public sealed record StartFastingHttpRequest(string Protocol, int? PlannedDurationHours = null, string? Notes = null);
