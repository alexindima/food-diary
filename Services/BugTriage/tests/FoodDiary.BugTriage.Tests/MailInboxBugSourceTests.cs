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
