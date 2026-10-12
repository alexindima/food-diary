using FoodDiary.BugTriage.Application.AuditRuns;

namespace FoodDiary.BugTriage.Application.Abstractions;

public interface IAuditRunStore {
    Task<bool> StartAsync(AuditRunId id, AuditWindow window, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(AuditRunId id, AuditCompletion completion, DateTimeOffset now, CancellationToken cancellationToken);
    Task<AuditRunSummary?> GetLatestAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task PurgeAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
