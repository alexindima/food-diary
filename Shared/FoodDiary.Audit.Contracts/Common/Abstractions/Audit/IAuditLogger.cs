using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Audit.Contracts.Common.Abstractions.Audit;

public interface IAuditLogger {
    void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null);
}
