using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingCyclicTransitionPlanner {
    public static bool CanTransition(FastingPlan plan, FastingOccurrence current) =>
        plan.Type == FastingPlanType.Cyclic &&
        (current.Kind == FastingOccurrenceKind.FastDay || current.Kind == FastingOccurrenceKind.EatDay);

    public static FastingOccurrence CreateAfterSkip(
        FastingPlan plan,
        FastingOccurrence current,
        UserId userId,
        DateTime startedAtUtc) {
        FastingOccurrenceKind nextKind = current.Kind == FastingOccurrenceKind.FastDay
            ? FastingOccurrenceKind.EatDay
            : FastingOccurrenceKind.FastDay;
        int nextSequenceNumber = ResolveSkipNextSequenceNumber(plan, current, nextKind);

        return CreateNextOccurrence(plan, current, userId, nextKind, nextSequenceNumber, startedAtUtc);
    }

    public static FastingOccurrence CreateAfterPostpone(
        FastingPlan plan,
        FastingOccurrence current,
        UserId userId,
        DateTime startedAtUtc) {
        FastingOccurrenceKind nextKind = ResolvePostponeNextKind(plan, current);
        int nextSequenceNumber = ResolvePostponeNextSequenceNumber(plan, current, nextKind);

        return CreateNextOccurrence(plan, current, userId, nextKind, nextSequenceNumber, startedAtUtc);
    }

    private static FastingOccurrence CreateNextOccurrence(
        FastingPlan plan,
        FastingOccurrence current,
        UserId userId,
        FastingOccurrenceKind nextKind,
        int nextSequenceNumber,
        DateTime startedAtUtc) {
        int? nextTargetHours = nextKind == FastingOccurrenceKind.FastDay
            ? 24
            : plan.CyclicEatDayEatingWindowHours;

        return FastingOccurrence.Create(
            plan.Id,
            userId,
            nextKind,
            startedAtUtc,
            nextSequenceNumber,
            targetHours: nextTargetHours,
            notes: current.Notes);
    }

    private static FastingOccurrenceKind ResolvePostponeNextKind(FastingPlan plan, FastingOccurrence current) {
        if (current.Kind == FastingOccurrenceKind.EatDay) {
            int phaseFastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
            int phaseEatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
            int phaseTotalCycleDays = phaseFastDays + phaseEatDays;
            int phaseOverallCycleDay = ((Math.Max(1, current.SequenceNumber) - 1) % phaseTotalCycleDays) + 1;
            int eatCycleDay = phaseOverallCycleDay <= phaseFastDays ? 1 : phaseOverallCycleDay - phaseFastDays;

            return eatCycleDay >= phaseEatDays
                ? FastingOccurrenceKind.FastDay
                : FastingOccurrenceKind.EatDay;
        }

        int fastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
        int eatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
        int totalCycleDays = fastDays + eatDays;
        int overallCycleDay = ((Math.Max(1, current.SequenceNumber) - 1) % totalCycleDays) + 1;
        int fastCycleDay = Math.Min(overallCycleDay, fastDays);

        return fastCycleDay >= fastDays
            ? FastingOccurrenceKind.EatDay
            : FastingOccurrenceKind.FastDay;
    }

    private static int ResolveSkipNextSequenceNumber(FastingPlan plan, FastingOccurrence current, FastingOccurrenceKind nextKind) {
        (int fastDays, _, int totalCycleDays, int cycleStartSequence) = GetCyclePosition(plan, current);

        return nextKind == FastingOccurrenceKind.FastDay
            ? cycleStartSequence + totalCycleDays
            : cycleStartSequence + fastDays;
    }

    private static int ResolvePostponeNextSequenceNumber(FastingPlan plan, FastingOccurrence current, FastingOccurrenceKind nextKind) {
        (int fastDays, _, int totalCycleDays, int cycleStartSequence) = GetCyclePosition(plan, current);

        if (current.Kind == FastingOccurrenceKind.FastDay) {
            return nextKind == FastingOccurrenceKind.FastDay
                ? current.SequenceNumber + 1
                : cycleStartSequence + fastDays;
        }

        return nextKind == FastingOccurrenceKind.EatDay
            ? current.SequenceNumber + 1
            : cycleStartSequence + totalCycleDays;
    }

    private static (int FastDays, int EatDays, int TotalCycleDays, int CycleStartSequence) GetCyclePosition(
        FastingPlan plan,
        FastingOccurrence current) {
        int fastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
        int eatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
        int totalCycleDays = fastDays + eatDays;
        int overallCycleDay = ((Math.Max(1, current.SequenceNumber) - 1) % totalCycleDays) + 1;
        int cycleStartSequence = current.SequenceNumber - (overallCycleDay - 1);

        return (fastDays, eatDays, totalCycleDays, cycleStartSequence);
    }
}
