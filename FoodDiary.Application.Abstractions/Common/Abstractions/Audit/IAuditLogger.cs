using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Common.Abstractions.Audit;

public interface IAuditLogger {
    void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null);
}
