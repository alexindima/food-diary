using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Models;

public sealed record FastingCheckInReadModel(
    FastingCheckInId Id,
    FastingOccurrenceId OccurrenceId,
    DateTime CheckedInAtUtc,
    int HungerLevel,
    int EnergyLevel,
    int MoodLevel,
    string? Symptoms,
    string? Notes);
