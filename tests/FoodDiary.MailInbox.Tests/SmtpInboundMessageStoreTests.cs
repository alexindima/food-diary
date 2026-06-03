using System.Buffers;
using System.Text;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;

namespace FoodDiary.MailInbox.Tests;

public sealed class SmtpInboundMessageStoreTests {
    [Fact]
    public async Task SaveAsync_WhenMessageHasToRecipients_StoresParsedInboundMessage() {
        var store = new RecordingInboundMailStore();
        var messageStore = new SmtpInboundMessageStore(store, NullLogger<SmtpInboundMessageStore>.Instance);
        var rawMime = CreateRawMime(includeToHeader: true);

        var response = await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["fallback@fooddiary.club"]),
            new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(rawMime)),
            CancellationToken.None);

        Assert.Equal(SmtpResponse.Ok.ReplyCode, response.ReplyCode);
        Assert.NotNull(store.LastSaved);
        Assert.Equal("sender@example.com", store.LastSaved.FromAddress);
        Assert.Equal(["admin@fooddiary.club"], store.LastSaved.ToRecipients);
        Assert.Equal("Hello", store.LastSaved.Subject);
        Assert.Contains("plain text", store.LastSaved.TextBody, StringComparison.Ordinal);
        Assert.Equal(rawMime, store.LastSaved.RawMime);
    }

    [Fact]
    public async Task SaveAsync_WhenMessageHasNoToRecipients_UsesTransactionRecipients() {
        var store = new RecordingInboundMailStore();
        var messageStore = new SmtpInboundMessageStore(store, NullLogger<SmtpInboundMessageStore>.Instance);
        var rawMime = CreateRawMime(includeToHeader: false);

        await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["fallback@fooddiary.club"]),
            new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(rawMime)),
            CancellationToken.None);

        Assert.NotNull(store.LastSaved);
        Assert.Equal(["fallback@fooddiary.club"], store.LastSaved.ToRecipients);
    }

    private static string CreateRawMime(bool includeToHeader) {
        var message = new MimeMessage();
        message.MessageId = "message-id";
        message.From.Add(MailboxAddress.Parse("sender@example.com"));
        if (includeToHeader) {
            message.To.Add(MailboxAddress.Parse("admin@fooddiary.club"));
        }

        message.Subject = "Hello";
        message.Body = new TextPart("plain") {
            Text = "plain text"
        };

        using var stream = new MemoryStream();
        message.WriteTo(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class TestMessageTransaction(IEnumerable<string> recipients) : IMessageTransaction {
        public IMailbox From { get; set; } = new Mailbox("sender", "example.com");

        public IList<IMailbox> To { get; } = recipients
            .Select(static recipient => {
                var parts = recipient.Split('@', 2);
                return (IMailbox)new Mailbox(parts[0], parts[1]);
            })
            .ToArray();

        public IReadOnlyDictionary<string, string> Parameters { get; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private sealed class RecordingInboundMailStore : IInboundMailStore {
        public InboundMailMessage? LastSaved { get; private set; }

        public Task<Guid> SaveAsync(InboundMailMessage message, CancellationToken cancellationToken) {
            LastSaved = message;
            return Task.FromResult(Guid.NewGuid());
        }

        public Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(
            int limit,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
