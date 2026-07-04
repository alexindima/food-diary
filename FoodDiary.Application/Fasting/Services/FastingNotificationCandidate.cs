using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Services;

internal sealed record FastingNotificationCandidate(
    UserId UserId,
    string Type,
    string ReferenceId,
    string? PlanType,
    string? OccurrenceKind) {
    public static FastingNotificationCandidate Create(
        FastingOccurrence occurrence,
        FastingPlan? plan,
        string type,
        string referenceId) =>
        new(
            occurrence.UserId,
            type,
            referenceId,
            plan?.Type.ToString(),
            occurrence.Kind.ToString());
}
