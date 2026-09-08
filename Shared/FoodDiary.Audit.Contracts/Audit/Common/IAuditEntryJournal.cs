using FoodDiary.Application.Abstractions.Audit.Models;

namespace FoodDiary.Application.Abstractions.Audit.Common;

public interface IAuditEntryJournal {
    Task<AuditEntryPage> GetPageAsync(AuditEntryFilter filter, CancellationToken cancellationToken);
}
