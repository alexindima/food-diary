using FoodDiary.BugTriage.Application.Reports;

namespace FoodDiary.BugTriage.Application.Abstractions;

public interface IBugReportStore {
    Task<IReadOnlyList<ReportSummary>> GetRecentAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> ContainsAsync(Guid sourceMessageId, CancellationToken cancellationToken);
    Task ImportAsync(ImportedReport report, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken);
    Task<ReportLease?> ClaimAsync(DateTimeOffset now, TimeSpan duration, int maxAttempts, CancellationToken cancellationToken);
    Task<bool> RenewAsync(Guid id, Guid token, DateTimeOffset now, TimeSpan duration, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(Guid id, Guid token, ReportCompletion completion, DateTimeOffset now, CancellationToken cancellationToken);
    Task<byte[]?> GetMimeAsync(Guid id, Guid token, DateTimeOffset now, CancellationToken cancellationToken);
    Task PurgeAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
