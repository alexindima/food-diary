using System.Runtime.CompilerServices;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Application.Admin.Services;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class BugAcknowledgementServiceTests {
    [Fact]
    public async Task QueuesEditableTemplateThenRecordsReceipt() {
        var candidate = new BugAcknowledgementCandidate(Guid.NewGuid(), "reporter@example.com", "original@example.com", "bug-ack:123", "ru");
        IBugAcknowledgementSource source = Substitute.For<IBugAcknowledgementSource>();
        source.ReadAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(Candidates(candidate));
        IEmailTemplateAdministrationReadService templates = Substitute.For<IEmailTemplateAdministrationReadService>();
        templates.GetTemplatesAsync(Arg.Any<CancellationToken>()).Returns([new EmailTemplateReadModel(Guid.NewGuid(), "bug_report_received", "ru", "Custom {{brand}}", "<p>Custom</p>", "Custom text", IsActive: true, DateTime.UtcNow, ModifiedOnUtc: null)]);
        IEmailTransport transport = Substitute.For<IEmailTransport>();
        IBugAcknowledgementReceipts receipts = Substitute.For<IBugAcknowledgementReceipts>();
        var service = new BugAcknowledgementService(source, templates, transport, receipts);
        await service.RunAsync(DateTimeOffset.UtcNow, CancellationToken.None);
        await transport.Received(1).SendAsync(Arg.Is<EmailMessage>(x => x.FromAddress == "no-reply@fooddiary.club" && x.ReplyTo == "bugs@fooddiary.club" && x.AutoSubmitted && x.InReplyTo == candidate.MessageId && x.Subject == "Custom FoodDiary" && x.IdempotencyKey == candidate.IdempotencyKey), Arg.Any<CancellationToken>());
        await receipts.Received(1).RecordAsync(candidate.InboxId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailedDispatchDoesNotRecordReceipt() {
        var candidate = new BugAcknowledgementCandidate(Guid.NewGuid(), "reporter@example.com", MessageId: null, "bug-ack:123", "en");
        IBugAcknowledgementSource source = Substitute.For<IBugAcknowledgementSource>();
        source.ReadAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(Candidates(candidate));
        IEmailTemplateAdministrationReadService templates = Substitute.For<IEmailTemplateAdministrationReadService>();
        templates.GetTemplatesAsync(Arg.Any<CancellationToken>()).Returns([new EmailTemplateReadModel(Guid.NewGuid(), "bug_report_received", "en", "Subject", "<p>Text</p>", "Text", IsActive: true, DateTime.UtcNow, ModifiedOnUtc: null)]);
        IEmailTransport transport = Substitute.For<IEmailTransport>();
        transport.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new HttpRequestException("Unavailable")));
        IBugAcknowledgementReceipts receipts = Substitute.For<IBugAcknowledgementReceipts>();
        var service = new BugAcknowledgementService(source, templates, transport, receipts);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.RunAsync(DateTimeOffset.UtcNow, CancellationToken.None));
        await receipts.DidNotReceive().RecordAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static async IAsyncEnumerable<BugAcknowledgementCandidate> Candidates(BugAcknowledgementCandidate candidate, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        yield return candidate;
    }
}
