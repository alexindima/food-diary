using FoodDiary.BugTriage.Application.Abstractions;

namespace FoodDiary.BugTriage.Application.Reports;

public sealed class ImportBugReports(IBugMailSource source, IBugReportStore store, TimeProvider timeProvider) {
    public async Task RunAsync(TimeSpan retention, CancellationToken cancellationToken) {
        await store.PurgeAsync(timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        await foreach (ImportedReport report in source.ReadNewAsync(cancellationToken).ConfigureAwait(false)) {
            DateTimeOffset expires = report.ReceivedAtUtc.Add(retention);
            ImportedReport retained = expires > timeProvider.GetUtcNow()
                ? report : report with { Subject = string.Empty, TextBody = string.Empty, RawMime = null };
            await store.ImportAsync(retained, expires, cancellationToken).ConfigureAwait(false);
        }
    }
}
