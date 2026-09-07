using System.Text.Json;
using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Presentation.Features.Reports;
using FoodDiary.MailInbox.Client.Models;

namespace FoodDiary.BugTriage.Tests;

public sealed class ContractSnapshotTests {
    [Fact]
    public void PublicPayloads_MatchReviewedSnapshot() {
        using var snapshot = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "bugtriage-mail-export-contract.json")));
        var samples = new Dictionary<string, object>(StringComparer.Ordinal) {
            ["claim"] = new ReportLease(Guid.Empty, Guid.Empty, "Subject", "Body", Guid.Empty, DateTimeOffset.UnixEpoch, 1, DateTimeOffset.UnixEpoch),
            ["completion"] = new CompleteReportHttpRequest(Guid.Empty, "not_confirmed", "Summary", MergeRequestUrl: null),
            ["summary"] = new ReportSummary(Guid.Empty, "pending", 0, Summary: null, MergeRequestUrl: null),
            ["mailExportEntry"] = new MailInboxExportEntryResponse(Guid.Empty, DateTimeOffset.UnixEpoch, ContentAvailable: true),
        };
        foreach ((string name, object sample) in samples) {
            JsonElement payload = JsonSerializer.SerializeToElement(sample, JsonSerializerOptions.Web);
            Assert.Equal(snapshot.RootElement.GetProperty(name).EnumerateArray().Select(p => p.GetString()),
                payload.EnumerateObject().Select(p => p.Name), StringComparer.Ordinal);
        }
    }
}
