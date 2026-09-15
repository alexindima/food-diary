using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Models;

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
