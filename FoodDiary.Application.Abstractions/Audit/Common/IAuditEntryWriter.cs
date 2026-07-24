using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Audit.Common;

public interface IAuditEntryWriter {
    Task AddAsync(
        UserId actorUserId,
        Guid? subjectClientUserId,
        string action,
        string targetType,
        string? targetId,
        string? metadata,
        CancellationToken cancellationToken = default);
}
