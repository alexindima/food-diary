namespace FoodDiary.Application.Abstractions.Fasting.Models;

public sealed record FastingCheckInModel(
    Guid Id,
    DateTime CheckedInAtUtc,
    int HungerLevel,
    int EnergyLevel,
    int MoodLevel,
    IReadOnlyList<string> Symptoms,
    string? Notes);
