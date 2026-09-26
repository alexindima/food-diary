using FoodDiary.Audit.Contracts.Audit.Models;

namespace FoodDiary.Audit.Contracts.Audit.Common;

public interface IAuditEntryJournal {
    Task<AuditEntryPage> GetPageAsync(AuditEntryFilter filter, CancellationToken cancellationToken);
}
