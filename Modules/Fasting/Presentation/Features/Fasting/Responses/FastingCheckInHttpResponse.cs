namespace FoodDiary.Presentation.Api.Features.Fasting.Responses;

public sealed record FastingCheckInHttpResponse(
    Guid Id,
    DateTime CheckedInAtUtc,
    int HungerLevel,
    int EnergyLevel,
    int MoodLevel,
    IReadOnlyList<string> Symptoms,
    string? Notes);
