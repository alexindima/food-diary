using System.Runtime.CompilerServices;
using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using NSubstitute;

namespace FoodDiary.BugTriage.Tests;

[ExcludeFromCodeCoverage]
public sealed class ImportBugReportsTests {
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task RunAsync_PurgesFirstAndRetainsContentOnlyBeforeExpiry(int secondsUntilExpiry, bool retainContent) {
        var retention = TimeSpan.FromDays(30);
        var report = new ImportedReport(Guid.NewGuid(), Now.Subtract(retention).AddSeconds(secondsUntilExpiry), "subject", "body", [0, 255]);
        IBugReportStore store = Substitute.For<IBugReportStore>();
        var source = new ReportSource(report, store);
        using var cancellation = new CancellationTokenSource();
        var importer = new ImportBugReports(source, store, new FixedClock());

        await importer.RunAsync(retention, cancellation.Token);

        await store.Received(1).PurgeAsync(Now, cancellation.Token);
        await store.Received(1).ImportAsync(Arg.Is<ImportedReport>(r =>
                r.SourceMessageId == report.SourceMessageId && r.ReceivedAtUtc == report.ReceivedAtUtc &&
                r.Subject == (retainContent ? report.Subject : string.Empty) &&
                r.TextBody == (retainContent ? report.TextBody : string.Empty) &&
                r.RawMime == (retainContent ? report.RawMime : null)),
            Now.AddSeconds(secondsUntilExpiry), cancellation.Token);
    }

    [Fact]
    public async Task RunAsync_DoesNotReadMailWhenPurgeFails() {
        IBugReportStore store = Substitute.For<IBugReportStore>();
        IBugMailSource source = Substitute.For<IBugMailSource>();
        var failure = new InvalidOperationException("storage unavailable");
        store.PurgeAsync(Now, Arg.Any<CancellationToken>()).Returns(Task.FromException(failure));

        var importer = new ImportBugReports(source, store, new FixedClock());
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => importer.RunAsync(TimeSpan.FromDays(30), CancellationToken.None)));
        source.DidNotReceiveWithAnyArgs().ReadNewAsync(default);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [ExcludeFromCodeCoverage]
    private sealed class ReportSource(ImportedReport report, IBugReportStore store) : IBugMailSource {
        public async IAsyncEnumerable<ImportedReport> ReadNewAsync([EnumeratorCancellation] CancellationToken cancellationToken) {
            await store.Received(1).PurgeAsync(Now, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            yield return report;
        }
    }
}
