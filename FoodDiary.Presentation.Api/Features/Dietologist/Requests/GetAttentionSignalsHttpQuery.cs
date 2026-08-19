using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record GetAttentionSignalsHttpQuery(
    [OpenApiNumericRange(1, 30)] int InactivityDays = 3,
    [OpenApiNumericRange(5, 100)] double CalorieDeviationPercent = 25,
    [OpenApiNumericRange(2, 14)] int SustainedDays = 3,
    [OpenApiNumericRange(0.5, 20)] double WeightChangePercent = 3,
    [OpenApiNumericRange(7, 90)] int LookbackDays = 14);
