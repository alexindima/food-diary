using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Models;

public sealed record FastingPlanReadModel(
    FastingPlanId Id,
    UserId UserId,
    FastingPlanType Type,
    FastingPlanStatus Status,
    FastingProtocol? Protocol,
    string? Title,
    DateTime StartedAtUtc,
    DateTime? StoppedAtUtc,
    int? IntermittentFastHours,
    int? IntermittentEatingWindowHours,
    int? ExtendedTargetHours,
    int? CyclicFastDays,
    int? CyclicEatDays,
    int? CyclicEatDayFastHours,
    int? CyclicEatDayEatingWindowHours,
    DateTime? CyclicAnchorDateUtc,
    DateTime? CyclicNextPhaseDateUtc);
