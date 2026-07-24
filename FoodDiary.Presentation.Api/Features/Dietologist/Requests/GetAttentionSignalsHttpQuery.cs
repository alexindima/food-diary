namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record GetAttentionSignalsHttpQuery(
    int InactivityDays = 3,
    double CalorieDeviationPercent = 25,
    int SustainedDays = 3,
    double WeightChangePercent = 3,
    int LookbackDays = 14);
