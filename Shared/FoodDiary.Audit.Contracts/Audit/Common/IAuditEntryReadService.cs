using FoodDiary.Audit.Contracts.Audit.Models;
namespace FoodDiary.Audit.Contracts.Audit.Common;

public interface IAuditEntryReadService {
    Task<IReadOnlyList<AuditEntryReadModel>> GetRecentAsync(
        Guid? subjectClientUserId,
        int limit,
        CancellationToken cancellationToken = default);
}
