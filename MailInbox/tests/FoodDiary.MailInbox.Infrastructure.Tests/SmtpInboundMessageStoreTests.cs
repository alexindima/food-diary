using System.Buffers;
using System.Text;
using System.Diagnostics.Metrics;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Application.Telemetry;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;

namespace FoodDiary.MailInbox.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class SmtpInboundMessageStoreTests {
    [Fact]
    public async Task SaveAsync_RecordsBoundedTelemetryWithoutMessageMetadata() {
        var measurements = new List<(string Instrument, string Outcome)>();
        using var listener = new MeterListener {
            InstrumentPublished = (instrument, meterListener) => {
                if (string.Equals(instrument.Meter.Name, MailInboxTelemetry.MeterName, StringComparison.Ordinal)) {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
            measurements.Add((instrument.Name, GetOutcome(tags))));
        listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
            measurements.Add((instrument.Name, GetOutcome(tags))));
        listener.Start();
        var messageStore = new SmtpInboundMessageStore(
            new RecordingInboundMailStore(),
            FixedTime,
            NullLogger<SmtpInboundMessageStore>.Instance);

        await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["private-recipient@fooddiary.club"]),
            new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(CreateRawMime(includeToHeader: true))),
            CancellationToken.None);

        Assert.Contains(measurements, static value =>
            value is ("fooddiary.mailinbox.ingestion.events", "success"));
        Assert.Contains(measurements, static value =>
            value is ("fooddiary.mailinbox.ingestion.duration_ms", "success"));
        Assert.Contains(measurements, static value =>
            value is ("fooddiary.mailinbox.message.size_bytes", "success"));
    }

    [Fact]
    public async Task SaveAsync_WhenMessageHasToRecipients_StoresEnvelopeRecipients() {
        var store = new RecordingInboundMailStore();
        var messageStore = new SmtpInboundMessageStore(store, FixedTime, NullLogger<SmtpInboundMessageStore>.Instance);
        string rawMime = CreateRawMime(includeToHeader: true);

        SmtpResponse response = await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["envelope@fooddiary.club"]),
            new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(rawMime)),
            CancellationToken.None);

        Assert.Equal(SmtpResponse.Ok.ReplyCode, response.ReplyCode);
        Assert.NotNull(store.LastSaved);
        Assert.Equal("sender@example.com", store.LastSaved.FromAddress);
        Assert.Equal(["envelope@fooddiary.club"], store.LastSaved.ToRecipients);
        Assert.Equal("Hello", store.LastSaved.Subject);
        Assert.Contains("plain text", store.LastSaved.TextBody, StringComparison.Ordinal);
        Assert.Equal(rawMime, store.LastSaved.RawMime);
        Assert.Equal(FixedNow, store.LastSaved.ReceivedAtUtc);
    }

    [Fact]
    public async Task SaveAsync_WhenMessageHasNoToRecipients_UsesTransactionRecipients() {
        var store = new RecordingInboundMailStore();
        var messageStore = new SmtpInboundMessageStore(store, FixedTime, NullLogger<SmtpInboundMessageStore>.Instance);
        string rawMime = CreateRawMime(includeToHeader: false);

        await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["fallback@fooddiary.club"]),
            new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(rawMime)),
            CancellationToken.None);

        Assert.NotNull(store.LastSaved);
        Assert.Equal(["fallback@fooddiary.club"], store.LastSaved.ToRecipients);
    }

    [Fact]
    public async Task SaveAsync_WhenTransactionRecipientsAreEmpty_UsesMimeToHeaderRecipients() {
        var store = new RecordingInboundMailStore();
        var messageStore = new SmtpInboundMessageStore(store, FixedTime, NullLogger<SmtpInboundMessageStore>.Instance);
        string rawMime = CreateRawMime(includeToHeader: true);

        await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction([]),
            new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(rawMime)),
            CancellationToken.None);

        Assert.NotNull(store.LastSaved);
        Assert.Equal(["admin@fooddiary.club"], store.LastSaved.ToRecipients);
    }

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 18, 11, 30, 0, TimeSpan.Zero);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    private static string CreateRawMime(bool includeToHeader) {
        var message = new MimeMessage();
        message.MessageId = "message-id";
        message.From.Add(MailboxAddress.Parse("sender@example.com"));
        if (includeToHeader) {
            message.To.Add(MailboxAddress.Parse("admin@fooddiary.club"));
        }

        message.Subject = "Hello";
        message.Body = new TextPart("plain") {
            Text = "plain text",
        };

        using var stream = new MemoryStream();
        message.WriteTo(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string GetOutcome(ReadOnlySpan<KeyValuePair<string, object?>> tags) {
        Assert.Single(tags.ToArray());
        KeyValuePair<string, object?> tag = tags[0];
        Assert.Equal("fooddiary.mailinbox.outcome", tag.Key);
        return Assert.IsType<string>(tag.Value);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestMessageTransaction(IEnumerable<string> recipients) : IMessageTransaction {
        public IMailbox From { get; set; } = new Mailbox("sender", "example.com");

        public IList<IMailbox> To { get; } = recipients
            .Select(static recipient => {
                string[] parts = recipient.Split('@', 2);
                return (IMailbox)new Mailbox(parts[0], parts[1]);
            })
            .ToArray();

        public IReadOnlyDictionary<string, string> Parameters { get; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    [ExcludeFromCodeCoverage]
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

        public Task<bool> MarkAsReadAsync(Guid id, DateTimeOffset readAtUtc, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
