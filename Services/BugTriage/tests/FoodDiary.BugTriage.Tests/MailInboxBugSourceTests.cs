using System.Text;
using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Infrastructure.Mail;
using FoodDiary.BugTriage.Infrastructure.Options;
using FoodDiary.MailInbox.Client.Export;
using FoodDiary.MailInbox.Client.Models;
using NSubstitute;

namespace FoodDiary.BugTriage.Tests;

public sealed class MailInboxBugSourceTests {
    [Fact]
    public async Task Scan_RejectsNonAdvancingCursorInsteadOfLooping() {
        IMailInboxExportClient client = Substitute.For<IMailInboxExportClient>();
        var entry = new MailInboxExportEntryResponse(Guid.NewGuid(), DateTimeOffset.UtcNow, ContentAvailable: false);
        client.GetPageAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns([entry]);
        var source = new MailInboxBugSource(client, Substitute.For<IBugReportStore>(), Microsoft.Extensions.Options.Options.Create(new BugTriageOptions()));
        int seen = 0;

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await foreach (ImportedReport report in source.ReadNewAsync(CancellationToken.None).ConfigureAwait(false)) {
                Assert.Equal(entry.Id, report.SourceMessageId);
                seen++;
            }
        });

        Assert.Equal(2, seen);
        Assert.Contains("cursor did not advance", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "Unparseable email", "")]
    [InlineData("Subject: HTML\r\nContent-Type: text/html\r\n\r\n<p>bug</p>", "HTML", "<p>bug</p>")]
    [InlineData("MIME-Version: 1.0\r\nContent-Type: application/octet-stream\r\n\r\nabc", "(no subject)", "")]
    public async Task Scan_HandlesMalformedHtmlAndNonTextMime(string raw, string subject, string body) {
        IMailInboxExportClient client = Substitute.For<IMailInboxExportClient>();
        var entry = new MailInboxExportEntryResponse(Guid.NewGuid(), DateTimeOffset.UtcNow, ContentAvailable: true);
        client.GetPageAsync(Arg.Any<string>(), beforeReceivedAtUtc: null, beforeId: null, Arg.Any<CancellationToken>()).Returns([entry]);
        client.GetPageAsync(Arg.Any<string>(), entry.ReceivedAtUtc, entry.Id, Arg.Any<CancellationToken>()).Returns([]);
        byte[] mime = Encoding.UTF8.GetBytes(raw);
        client.GetMimeAsync(entry.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mime);
        var source = new MailInboxBugSource(client, Substitute.For<IBugReportStore>(), Microsoft.Extensions.Options.Options.Create(new BugTriageOptions()));
        var results = new List<ImportedReport>();

        await foreach (ImportedReport report in source.ReadNewAsync(CancellationToken.None)) { results.Add(report); }

        ImportedReport actual = Assert.Single(results);
        Assert.Multiple(
            () => Assert.Equal(subject, actual.Subject),
            () => Assert.Equal(body, actual.TextBody),
            () => Assert.Equal(mime, actual.RawMime));
    }

    [Fact]
    public async Task ScanCapsNewImports_AndResumesAfterPersistedReceipts() {
        IMailInboxExportClient client = Substitute.For<IMailInboxExportClient>();
        IBugReportStore store = Substitute.For<IBugReportStore>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var first = new MailInboxExportEntryResponse(Guid.NewGuid(), now, ContentAvailable: false);
        var second = new MailInboxExportEntryResponse(Guid.NewGuid(), now.AddMinutes(-1), ContentAvailable: false);
        client.GetPageAsync("bugs@fooddiary.club", beforeReceivedAtUtc: null, beforeId: null, Arg.Any<CancellationToken>()).Returns([first, second]);
        client.GetPageAsync("bugs@fooddiary.club", second.ReceivedAtUtc, second.Id, Arg.Any<CancellationToken>()).Returns([]);
        var source = new MailInboxBugSource(client, store,
            Microsoft.Extensions.Options.Options.Create(new BugTriageOptions { MaxImportsPerPoll = 1 }));
        var reports = new List<ImportedReport>();
        await foreach (ImportedReport report in source.ReadNewAsync(CancellationToken.None)) {
            reports.Add(report);
        }
        Assert.Equal(first.Id, Assert.Single(reports).SourceMessageId);
        store.ContainsAsync(first.Id, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        reports.Clear();
        await foreach (ImportedReport report in source.ReadNewAsync(CancellationToken.None)) {
            reports.Add(report);
        }
        Assert.Equal(second.Id, Assert.Single(reports).SourceMessageId);
    }

    [Fact]
    public async Task ScanTraversesPages_SkipsReceipts_AndHandlesPurgedContent() {
        IMailInboxExportClient client = Substitute.For<IMailInboxExportClient>();
        IBugReportStore store = Substitute.For<IBugReportStore>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var first = new MailInboxExportEntryResponse(Guid.NewGuid(), now, ContentAvailable: true);
        var second = new MailInboxExportEntryResponse(Guid.NewGuid(), now.AddMinutes(-1), ContentAvailable: false);
        var third = new MailInboxExportEntryResponse(Guid.NewGuid(), now.AddMinutes(-2), ContentAvailable: true);
        client.GetPageAsync("bugs@fooddiary.club", beforeReceivedAtUtc: null, beforeId: null, Arg.Any<CancellationToken>()).Returns([first, second]);
        client.GetPageAsync("bugs@fooddiary.club", second.ReceivedAtUtc, second.Id, Arg.Any<CancellationToken>()).Returns([third]);
        client.GetPageAsync("bugs@fooddiary.club", third.ReceivedAtUtc, third.Id, Arg.Any<CancellationToken>()).Returns([]);
        store.ContainsAsync(first.Id, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        byte[] mime = Encoding.UTF8.GetBytes("Subject: Bug\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nКнопка не работает");
        client.GetMimeAsync(third.Id, "bugs@fooddiary.club", Arg.Any<CancellationToken>()).Returns(mime);
        var source = new MailInboxBugSource(client, store, Microsoft.Extensions.Options.Options.Create(new BugTriageOptions()));
        var reports = new List<ImportedReport>();
        await foreach (ImportedReport report in source.ReadNewAsync(CancellationToken.None)) {
            reports.Add(report);
        }
        Assert.Equal(2, reports.Count);
        Assert.Null(reports[0].RawMime);
        Assert.Equal("Кнопка не работает", reports[1].TextBody);
        Assert.Equal(mime, reports[1].RawMime);
        await client.DidNotReceive().GetMimeAsync(first.Id, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
