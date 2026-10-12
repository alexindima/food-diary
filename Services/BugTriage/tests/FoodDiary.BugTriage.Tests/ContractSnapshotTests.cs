using System.Text.Json;
using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Application.AuditRuns;
using FoodDiary.BugTriage.Presentation.Features.AuditRuns;
using FoodDiary.BugTriage.Presentation.Features.Reports;
using FoodDiary.MailInbox.Client.Models;

namespace FoodDiary.BugTriage.Tests;

public sealed class ContractSnapshotTests {
    [Fact]
    public void PublicPayloads_MatchReviewedSnapshot() {
        using var snapshot = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "bugtriage-mail-export-contract.json")));
        var samples = new Dictionary<string, object>(StringComparer.Ordinal) {
            ["claim"] = new ReportLeaseHttpResponse(Guid.Empty, Guid.Empty, "Subject", "Body", Guid.Empty, DateTimeOffset.UnixEpoch, 1, DateTimeOffset.UnixEpoch),
            ["completion"] = new CompleteReportHttpRequest(Guid.Empty, "not_confirmed", "Summary", MergeRequestUrl: null),
            ["summary"] = new ReportSummary(Guid.Empty, "pending", 0, Summary: null, MergeRequestUrl: null),
            ["mailExportEntry"] = new MailInboxExportEntryResponse(Guid.Empty, DateTimeOffset.UnixEpoch, ContentAvailable: true),
            ["auditStart"] = new StartAuditRunHttpRequest(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1)),
            ["auditCompletion"] = new CompleteAuditRunHttpRequest("clean", "Summary", []),
            ["auditFinding"] = new AuditFinding(Guid.Empty, "Title", "needs_information", "Summary", MergeRequestUrl: null),
            ["journal"] = new BugReportJournalPage([], 0),
            ["journalEntry"] = new BugReportJournalEntry(Guid.Empty, SourceMessageId: null, DateTimeOffset.UnixEpoch, "Title", "failed", 1, Summary: null, MergeRequestUrl: null, ContentExpired: false),
            ["latestAudit"] = new AuditRunSummary(Guid.Empty, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), DateTimeOffset.UnixEpoch,
                CompletedAtUtc: null, "running", Summary: null, 0, ContentExpired: false, IsStale: false, LastSuccessfulAtUtc: null),
        };
        foreach ((string name, object sample) in samples) {
            JsonElement payload = JsonSerializer.SerializeToElement(sample, JsonSerializerOptions.Web);
            Assert.Equal(snapshot.RootElement.GetProperty(name).EnumerateArray().Select(p => p.GetString()),
                payload.EnumerateObject().Select(p => p.Name), StringComparer.Ordinal);
        }
    }
}
