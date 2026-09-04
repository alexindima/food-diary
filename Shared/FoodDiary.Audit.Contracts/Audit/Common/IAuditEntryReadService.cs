using FoodDiary.Application.Abstractions.Audit.Models;
namespace FoodDiary.Application.Abstractions.Audit.Common;

public interface IAuditEntryReadService {
    Task<IReadOnlyList<AuditEntryReadModel>> GetRecentAsync(
        Guid? subjectClientUserId,
        int limit,
        CancellationToken cancellationToken = default);
}
