using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Models;

public sealed record FastingCheckInReadModel(
    FastingCheckInId Id,
    FastingOccurrenceId OccurrenceId,
    DateTime CheckedInAtUtc,
    int HungerLevel,
    int EnergyLevel,
    int MoodLevel,
    string? Symptoms,
    string? Notes);
