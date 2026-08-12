using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingEndPolicy {
    public static void End(FastingPlan plan, FastingOccurrence occurrence, DateTime endedAtUtc) {
        switch (plan.Type) {
            case FastingPlanType.Cyclic:
                occurrence.Interrupt(endedAtUtc);
                break;
            case FastingPlanType.Intermittent:
                occurrence.Complete(endedAtUtc);
                break;
            default:
                EndExtendedOccurrence(occurrence, endedAtUtc);
                break;
        }

        plan.Stop(endedAtUtc);
    }

    private static void EndExtendedOccurrence(FastingOccurrence occurrence, DateTime endedAtUtc) {
        DateTime targetReachedAtUtc = occurrence.StartedAtUtc.AddHours(occurrence.TargetHours ?? 0);
        if (endedAtUtc >= targetReachedAtUtc) {
            occurrence.Complete(endedAtUtc);
            return;
        }

        occurrence.Interrupt(endedAtUtc);
    }
}
