using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Mappings;

public static class FastingMappings {
    private static readonly char[] SymptomSeparators = [','];

    public static FastingCheckInModel ToModel(this FastingCheckIn checkIn) =>
        new(
            checkIn.Id.Value,
            checkIn.CheckedInAtUtc,
            checkIn.HungerLevel,
            checkIn.EnergyLevel,
            checkIn.MoodLevel,
            ParseSymptoms(checkIn.Symptoms),
            checkIn.Notes);

    public static FastingSessionModel ToModel(this FastingSession session) =>
        new(
            session.Id.Value,
            session.StartedAtUtc,
            session.EndedAtUtc,
            session.InitialPlannedDurationHours,
            session.AddedDurationHours,
            session.PlannedDurationHours,
            session.Protocol.ToString(),
            ResolvePlanType(session.Protocol).ToString(),
            ResolveOccurrenceKind(session.Protocol).ToString(),
            null,
            null,
            null,
            null,
            null,
            null,
            session.IsCompleted,
            session.Status.ToString(),
            session.Notes,
            null,
            null,
            null,
            null,
            [],
            null,
            []);

    public static FastingSessionModel ToModel(this FastingOccurrence occurrence) {
        return occurrence.ToModel(occurrence.Plan, null);
    }

    public static FastingSessionModel ToModel(
        this FastingOccurrence occurrence,
        FastingPlan? plan,
        IReadOnlyList<FastingCheckIn>? checkIns = null) {
        var protocol = plan?.Protocol ?? FastingProtocol.Custom;
        var initialPlannedDurationHours = occurrence.InitialTargetHours ?? ResolveDefaultHours(occurrence, plan);
        var addedDurationHours = occurrence.AddedTargetHours;
        var plannedDurationHours = initialPlannedDurationHours + addedDurationHours;
        var isCompleted = occurrence.Status != FastingOccurrenceStatus.Active &&
            occurrence.Status != FastingOccurrenceStatus.Scheduled &&
            occurrence.Status != FastingOccurrenceStatus.Postponed;
        var cyclicPhaseProgress = ResolveCyclicPhaseProgress(occurrence, plan);
        var sortedCheckIns = (checkIns ?? [])
            .OrderByDescending(static checkIn => checkIn.CheckedInAtUtc)
            .ToList();
        var latestCheckIn = sortedCheckIns.FirstOrDefault();

        return new FastingSessionModel(
            occurrence.Id.Value,
            occurrence.StartedAtUtc,
            occurrence.EndedAtUtc,
            initialPlannedDurationHours,
            addedDurationHours,
            plannedDurationHours,
            protocol.ToString(),
            (plan?.Type ?? FastingPlanType.Extended).ToString(),
            occurrence.Kind.ToString(),
            plan?.CyclicFastDays,
            plan?.CyclicEatDays,
            plan?.CyclicEatDayFastHours,
            plan?.CyclicEatDayEatingWindowHours,
            cyclicPhaseProgress.DayNumber,
            cyclicPhaseProgress.DayTotal,
            isCompleted,
            occurrence.Status.ToString(),
            occurrence.Notes,
            latestCheckIn?.CheckedInAtUtc ?? occurrence.CheckInAtUtc,
            latestCheckIn?.HungerLevel ?? occurrence.HungerLevel,
            latestCheckIn?.EnergyLevel ?? occurrence.EnergyLevel,
            latestCheckIn?.MoodLevel ?? occurrence.MoodLevel,
            latestCheckIn is not null ? ParseSymptoms(latestCheckIn.Symptoms) : ParseSymptoms(occurrence.Symptoms),
            latestCheckIn?.Notes ?? occurrence.CheckInNotes,
            sortedCheckIns.Select(static checkIn => checkIn.ToModel()).ToList());
    }

    private static IReadOnlyList<string> ParseSymptoms(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return [];
        }

        return value
            .Split(SymptomSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int ResolveDefaultHours(FastingOccurrence occurrence, FastingPlan? plan) {
        if (plan?.Type == FastingPlanType.Cyclic) {
            return occurrence.Kind == FastingOccurrenceKind.EatDay || occurrence.Kind == FastingOccurrenceKind.EatingWindow
                ? plan.CyclicEatDayEatingWindowHours ?? 8
                : plan.CyclicEatDayFastHours ?? 16;
        }

        return FastingSession.GetDefaultDuration(plan?.Protocol ?? FastingProtocol.Custom);
    }

    private static FastingPlanType ResolvePlanType(FastingProtocol protocol) => protocol switch {
        FastingProtocol.F16_8 => FastingPlanType.Intermittent,
        FastingProtocol.F18_6 => FastingPlanType.Intermittent,
        FastingProtocol.F20_4 => FastingPlanType.Intermittent,
        FastingProtocol.CustomIntermittent => FastingPlanType.Intermittent,
        _ => FastingPlanType.Extended
    };

    private static FastingOccurrenceKind ResolveOccurrenceKind(FastingProtocol protocol) => ResolvePlanType(protocol) switch {
        FastingPlanType.Intermittent => FastingOccurrenceKind.FastingWindow,
        FastingPlanType.Cyclic => FastingOccurrenceKind.FastDay,
        _ => FastingOccurrenceKind.FastDay
    };

    private static (int? DayNumber, int? DayTotal) ResolveCyclicPhaseProgress(FastingOccurrence occurrence, FastingPlan? plan) {
        if (plan?.Type != FastingPlanType.Cyclic) {
            return (null, null);
        }

        if (occurrence.Kind != FastingOccurrenceKind.FastDay && occurrence.Kind != FastingOccurrenceKind.EatDay) {
            return (null, null);
        }

        var fastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
        var eatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
        var totalCycleDays = fastDays + eatDays;
        var overallCycleDay = ((Math.Max(1, occurrence.SequenceNumber) - 1) % totalCycleDays) + 1;

        if (occurrence.Kind == FastingOccurrenceKind.FastDay) {
            return (((overallCycleDay - 1) % fastDays) + 1, fastDays);
        }

        var eatCycleDay = overallCycleDay <= fastDays ? 1 : overallCycleDay - fastDays;
        return (((eatCycleDay - 1) % eatDays) + 1, eatDays);
    }
}
