using System.Text;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Infrastructure.Integrations.MailInbox;
using FoodDiary.MailInbox.Client;
using FoodDiary.MailInbox.Client.Export;
using FoodDiary.MailInbox.Client.Models;

namespace FoodDiary.Modules.Admin.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class BugAcknowledgementPagingTests {
    [Fact]
    public async Task Read_TraversesCursorAndStopsAtOlderMessage() {
        using var cancellation = new CancellationTokenSource();
        DateTimeOffset since = DateTimeOffset.UnixEpoch.AddDays(2);
        var first = new MailInboxExportEntryResponse(Guid.NewGuid(), since.AddDays(1), ContentAvailable: true);
        var last = new MailInboxExportEntryResponse(Guid.NewGuid(), since, ContentAvailable: true);
        var old = new MailInboxExportEntryResponse(Guid.NewGuid(), since.AddTicks(-1), ContentAvailable: true);
        IMailInboxExportClient export = Substitute.For<IMailInboxExportClient>();
        IMailInboxClient inbox = Substitute.For<IMailInboxClient>();
        IBugAcknowledgementReceipts receipts = Substitute.For<IBugAcknowledgementReceipts>();
        export.GetPageAsync("bugs@fooddiary.club", beforeReceivedAtUtc: null, beforeId: null, cancellation.Token).Returns([first]);
        export.GetPageAsync("bugs@fooddiary.club", first.ReceivedAtUtc, first.Id, cancellation.Token).Returns([last, old]);
        foreach (MailInboxExportEntryResponse entry in new[] { first, last }) {
            inbox.GetMessageAsync(entry.Id, cancellation.Token).Returns(Details(entry.Id));
            export.GetMimeAsync(entry.Id, "bugs@fooddiary.club", cancellation.Token).Returns(Mime("person@example.com"));
        }
        var source = new BugAcknowledgementSource(export, inbox, receipts);
        var candidates = new List<BugAcknowledgementCandidate>();
        await foreach (BugAcknowledgementCandidate candidate in source.ReadAsync(since, cancellation.Token)) { candidates.Add(candidate); }
        Assert.Equal(new[] { first.Id, last.Id }, candidates.Select(x => x.InboxId));
        await inbox.DidNotReceive().GetMessageAsync(old.Id, Arg.Any<CancellationToken>());
        await export.Received(2).GetPageAsync("bugs@fooddiary.club", Arg.Any<DateTimeOffset?>(), Arg.Any<Guid?>(), cancellation.Token);
    }

    [Theory]
    [InlineData("unavailable")]
    [InlineData("receipt")]
    [InlineData("missing-details")]
    [InlineData("empty-envelope")]
    [InlineData("purged")]
    [InlineData("missing-mime")]
    [InlineData("automatic")]
    public async Task Read_SkipsIneligibleMessagesAndFinishesEmptyPage(string reason) {
        var entry = new MailInboxExportEntryResponse(Guid.NewGuid(), DateTimeOffset.UnixEpoch, ContentAvailable: !string.Equals(reason, "unavailable", StringComparison.Ordinal));
        IMailInboxExportClient export = Substitute.For<IMailInboxExportClient>();
        IMailInboxClient inbox = Substitute.For<IMailInboxClient>();
        IBugAcknowledgementReceipts receipts = Substitute.For<IBugAcknowledgementReceipts>();
        export.GetPageAsync("bugs@fooddiary.club", beforeReceivedAtUtc: null, beforeId: null, CancellationToken.None).Returns([entry]);
        export.GetPageAsync("bugs@fooddiary.club", entry.ReceivedAtUtc, entry.Id, CancellationToken.None).Returns([]);
        receipts.ContainsAsync(entry.Id, CancellationToken.None).Returns(string.Equals(reason, "receipt", StringComparison.Ordinal));
        InboundMailMessageDetailsResponse? details = string.Equals(reason, "missing-details", StringComparison.Ordinal) ? null : Details(entry.Id) with {
            EnvelopeFromAddress = string.Equals(reason, "empty-envelope", StringComparison.Ordinal) ? " " : "person@example.com",
            ContentPurgedAtUtc = string.Equals(reason, "purged", StringComparison.Ordinal) ? DateTimeOffset.UnixEpoch : null,
        };
        inbox.GetMessageAsync(entry.Id, CancellationToken.None).Returns(details);
        export.GetMimeAsync(entry.Id, "bugs@fooddiary.club", CancellationToken.None).Returns(string.Equals(reason, "missing-mime", StringComparison.Ordinal) ? null : Mime("person@example.com", string.Equals(reason, "automatic", StringComparison.Ordinal) ? "Auto-Submitted: auto-replied\r\n" : ""));
        var source = new BugAcknowledgementSource(export, inbox, receipts);
        var candidates = new List<BugAcknowledgementCandidate>();
        await foreach (BugAcknowledgementCandidate candidate in source.ReadAsync(DateTimeOffset.UnixEpoch, CancellationToken.None)) { candidates.Add(candidate); }
        Assert.Empty(candidates);
        if (reason is "unavailable" or "receipt") { await inbox.DidNotReceive().GetMessageAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()); }
    }

    [Fact]
    public async Task Read_RejectsNonAdvancingCursor() {
        var entry = new MailInboxExportEntryResponse(Guid.NewGuid(), DateTimeOffset.UnixEpoch, ContentAvailable: false);
        IMailInboxExportClient export = Substitute.For<IMailInboxExportClient>();
        export.GetPageAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns([entry]);
        var source = new BugAcknowledgementSource(export, Substitute.For<IMailInboxClient>(), Substitute.For<IBugAcknowledgementReceipts>());
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await foreach (BugAcknowledgementCandidate candidate in source.ReadAsync(DateTimeOffset.UnixEpoch, CancellationToken.None)) { Assert.Fail($"Unexpected candidate: {candidate.InboxId}"); }
        });
        Assert.Equal("MailInbox cursor did not advance.", error.Message);
    }

    [Theory]
    [InlineData("support@fooddiary.club")]
    [InlineData("no-reply@example.com")]
    [InlineData("noreply@example.com")]
    [InlineData("mailer-daemon@example.com")]
    [InlineData("postmaster@example.com")]
    public void Parse_RejectsSystemSenders(string sender) {
        Assert.Null(BugAcknowledgementSource.Parse(Guid.NewGuid(), sender, Mime(sender)));
    }

    [Fact]
    public void Parse_MissingMessageIdUsesInboxIdentityAndDetectsRussian() {
        byte[] bytes = Encoding.UTF8.GetBytes("From: person@example.com\r\nSubject: \u041e\u0448\u0438\u0431\u043a\u0430\r\n\r\nProblem");
        var id = Guid.NewGuid();
        BugAcknowledgementCandidate? first = BugAcknowledgementSource.Parse(id, "person@example.com", bytes);
        BugAcknowledgementCandidate? same = BugAcknowledgementSource.Parse(id, "PERSON@example.com", bytes);
        BugAcknowledgementCandidate? other = BugAcknowledgementSource.Parse(Guid.NewGuid(), "person@example.com", bytes);
        Assert.NotNull(first);
        Assert.NotNull(same);
        Assert.NotNull(other);
        Assert.Multiple(() => Assert.Equal("ru", first.Locale), () => Assert.Equal(first.IdempotencyKey, same.IdempotencyKey), () => Assert.NotEqual(first.IdempotencyKey, other.IdempotencyKey, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a MIME message")]
    public void Parse_MalformedMimeIsIgnored(string text) {
        Assert.Null(BugAcknowledgementSource.Parse(Guid.NewGuid(), "person@example.com", Encoding.UTF8.GetBytes(text)));
    }

    private static byte[] Mime(string sender, string headers = "") => Encoding.UTF8.GetBytes($"From: {sender}\r\nMessage-ID: <original@example.com>\r\nSubject: Bug\r\n{headers}\r\nProblem");
    private static InboundMailMessageDetailsResponse Details(Guid id) => new(id, "message", "person@example.com", ["bugs@fooddiary.club"], "subject", "text", "html", "raw", "general", "received", ReadAtUtc: null, DateTimeOffset.UnixEpoch, ContentPurgedAtUtc: null, DmarcReport: null, EnvelopeFromAddress: "person@example.com");
}
