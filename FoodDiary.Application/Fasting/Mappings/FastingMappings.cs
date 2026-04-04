using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Fasting.Mappings;

public static class FastingMappings {
    public static FastingSessionModel ToModel(this FastingSession session) =>
        new(
            session.Id.Value,
            session.StartedAtUtc,
            session.EndedAtUtc,
            session.PlannedDurationHours,
            session.Protocol.ToString(),
            session.IsCompleted,
            session.Notes);
}
