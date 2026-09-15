namespace FoodDiary.Modules.Fasting.Presentation.Contracts.Responses;

public sealed record FastingCheckInHttpResponse(
    Guid Id,
    DateTime CheckedInAtUtc,
    int HungerLevel,
    int EnergyLevel,
    int MoodLevel,
    IReadOnlyList<string> Symptoms,
    string? Notes);
