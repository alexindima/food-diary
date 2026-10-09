using FoodDiary.BugTriage.Application.Reports.Identifiers;
using FoodDiary.BugTriage.Application.Reports;

namespace FoodDiary.BugTriage.Application.Abstractions;

public interface IBugReportStore {
    Task<IReadOnlyList<ReportSummary>> GetRecentAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> ContainsAsync(SourceMessageId sourceMessageId, CancellationToken cancellationToken);
    Task ImportAsync(ImportedReport report, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken);
    Task<ReportLease?> ClaimAsync(DateTimeOffset now, TimeSpan duration, int maxAttempts, CancellationToken cancellationToken);
    Task<bool> RenewAsync(BugReportId id, LeaseToken token, DateTimeOffset now, TimeSpan duration, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(BugReportId id, LeaseToken token, ReportCompletion completion, DateTimeOffset now, CancellationToken cancellationToken);
    Task<byte[]?> GetMimeAsync(BugReportId id, LeaseToken token, DateTimeOffset now, CancellationToken cancellationToken);
    Task PurgeAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
