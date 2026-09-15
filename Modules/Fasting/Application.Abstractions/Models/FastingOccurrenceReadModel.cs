using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Models;

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
