using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Models;

public sealed record FastingOccurrenceReadModel(
    FastingOccurrenceId Id,
    FastingPlanId PlanId,
    FastingPlanReadModel? Plan,
    UserId UserId,
    FastingOccurrenceKind Kind,
    FastingOccurrenceStatus Status,
    int SequenceNumber,
    DateTime? ScheduledForUtc,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    int? InitialTargetHours,
    int AddedTargetHours,
    string? Notes,
    DateTime? CheckInAtUtc,
    int? HungerLevel,
    int? EnergyLevel,
    int? MoodLevel,
    string? Symptoms,
    string? CheckInNotes) {
    public int? TargetHours => InitialTargetHours + AddedTargetHours;
}
