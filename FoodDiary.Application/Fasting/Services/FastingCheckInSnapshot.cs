namespace FoodDiary.Application.Fasting.Services;

internal sealed record FastingCheckInSnapshot(
    DateTime CheckedInAtUtc,
    int HungerLevel,
    int EnergyLevel,
    int MoodLevel,
    IReadOnlyList<string> Symptoms,
    string? Notes);
