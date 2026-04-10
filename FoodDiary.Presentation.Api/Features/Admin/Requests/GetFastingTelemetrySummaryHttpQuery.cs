namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetFastingTelemetrySummaryHttpQuery(
    int Hours = 24);
