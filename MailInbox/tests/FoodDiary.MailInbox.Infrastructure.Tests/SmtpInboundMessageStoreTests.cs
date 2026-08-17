using System.Buffers;
using System.Text;
using System.Diagnostics.Metrics;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Application.Telemetry;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Infrastructure.Services;
using FoodDiary.MailInbox.Infrastructure.Options;
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
            Microsoft.Extensions.Options.Options.Create(new MailInboxSmtpOptions()),
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
        SmtpInboundMessageStore messageStore = CreateMessageStore(store);
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
        Assert.True(store.LastSaved.RawMimeBytes.Span.SequenceEqual(Encoding.UTF8.GetBytes(rawMime)));
        Assert.Equal(FixedNow, store.LastSaved.ReceivedAtUtc);
    }

    [Fact]
    public async Task SaveAsync_WhenMessageHasNoToRecipients_UsesTransactionRecipients() {
        var store = new RecordingInboundMailStore();
        SmtpInboundMessageStore messageStore = CreateMessageStore(store);
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
        SmtpInboundMessageStore messageStore = CreateMessageStore(store);
        string rawMime = CreateRawMime(includeToHeader: true);

        await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction([]),
            new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(rawMime)),
            CancellationToken.None);

        Assert.NotNull(store.LastSaved);
        Assert.Equal(["admin@fooddiary.club"], store.LastSaved.ToRecipients);
    }

    [Fact]
    public async Task SaveAsync_PreservesBinaryMimeBytesWithoutUtf8RoundTrip() {
        var store = new RecordingInboundMailStore();
        SmtpInboundMessageStore messageStore = CreateMessageStore(store);
        byte[] rawBytes = CreateBinaryRawMime();

        SmtpResponse response = await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["admin@fooddiary.club"]),
            new ReadOnlySequence<byte>(rawBytes),
            CancellationToken.None);

        Assert.Equal(SmtpResponse.Ok.ReplyCode, response.ReplyCode);
        Assert.NotNull(store.LastSaved);
        Assert.True(store.LastSaved.RawMimeBytes.Span.SequenceEqual(rawBytes));
    }

    [Fact]
    public async Task SaveAsync_WhenMimePartLimitIsExceeded_RejectsBeforePersistence() {
        var store = new RecordingInboundMailStore();
        SmtpInboundMessageStore messageStore = CreateMessageStore(
            store,
            new MailInboxSmtpOptions { MaxMimeParts = 1 });
        byte[] rawBytes = CreateMultipartRawMime(partCount: 2);

        SmtpResponse response = await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["admin@fooddiary.club"]),
            new ReadOnlySequence<byte>(rawBytes),
            CancellationToken.None);

        Assert.Equal(SmtpReplyCode.TransactionFailed, response.ReplyCode);
        Assert.Null(store.LastSaved);
    }

    [Fact]
    public async Task SaveAsync_WhenActualMessageSizeExceedsLimit_RejectsBeforeParsing() {
        var store = new RecordingInboundMailStore();
        SmtpInboundMessageStore messageStore = CreateMessageStore(
            store,
            new MailInboxSmtpOptions { MaxMessageSizeBytes = 10 });

        SmtpResponse response = await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["admin@fooddiary.club"]),
            new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("more than ten bytes")),
            CancellationToken.None);

        Assert.Equal(SmtpResponse.SizeLimitExceeded.ReplyCode, response.ReplyCode);
        Assert.Null(store.LastSaved);
    }

    [Fact]
    public async Task SaveAsync_WhenExtractedBodyExceedsLimit_TruncatesBeforePersistence() {
        var store = new RecordingInboundMailStore();
        SmtpInboundMessageStore messageStore = CreateMessageStore(
            store,
            new MailInboxSmtpOptions { MaxExtractedBodyCharacters = 5 });

        SmtpResponse response = await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["admin@fooddiary.club"]),
            new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(CreateRawMime(includeToHeader: true))),
            CancellationToken.None);

        Assert.Equal(SmtpResponse.Ok.ReplyCode, response.ReplyCode);
        Assert.NotNull(store.LastSaved);
        Assert.Equal("plain", store.LastSaved.TextBody);
    }

    [Fact]
    public async Task SaveAsync_WhenProcessingSlotsStayBusy_ReturnsTemporaryOverload() {
        var store = new BlockingInboundMailStore(expectedConcurrentCalls: 1);
        SmtpInboundMessageStore messageStore = CreateMessageStore(
            store,
            new MailInboxSmtpOptions {
                MaxConcurrentMessageProcessing = 1,
                ProcessingQueueTimeout = TimeSpan.FromMilliseconds(25),
            });
        byte[] rawBytes = Encoding.UTF8.GetBytes(CreateRawMime(includeToHeader: true));
        Task<SmtpResponse> first = messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["admin@fooddiary.club"]),
            new ReadOnlySequence<byte>(rawBytes),
            CancellationToken.None);
        await store.WaitUntilExpectedConcurrencyAsync()
            .WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        SmtpResponse overloaded = await messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["admin@fooddiary.club"]),
            new ReadOnlySequence<byte>(rawBytes),
            CancellationToken.None);

        store.Release();
        SmtpResponse firstResponse = await first;

        Assert.Equal(SmtpReplyCode.Overloaded, overloaded.ReplyCode);
        Assert.Equal(SmtpResponse.Ok.ReplyCode, firstResponse.ReplyCode);
    }

    [Fact]
    public async Task SaveAsync_WhenSeveralNearLimitMessagesArrive_BoundsMimeProcessingConcurrency() {
        const int maxConcurrentProcessing = 2;
        var store = new BlockingInboundMailStore(maxConcurrentProcessing);
        byte[] rawBytes = CreateNearLimitBinaryRawMime(10 * 1024 * 1024);
        SmtpInboundMessageStore messageStore = CreateMessageStore(
            store,
            new MailInboxSmtpOptions {
                MaxMessageSizeBytes = rawBytes.Length,
                MaxConcurrentMessageProcessing = maxConcurrentProcessing,
                ProcessingQueueTimeout = TimeSpan.FromSeconds(10),
            });

        Task<SmtpResponse>[] saves = [.. Enumerable.Range(0, 6).Select(_ => messageStore.SaveAsync(
            context: null!,
            new TestMessageTransaction(["admin@fooddiary.club"]),
            new ReadOnlySequence<byte>(rawBytes),
            CancellationToken.None))];

        await store.WaitUntilExpectedConcurrencyAsync()
            .WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

        Assert.Equal(maxConcurrentProcessing, store.ActiveCalls);
        Assert.Equal(maxConcurrentProcessing, store.MaxConcurrentCalls);
        Assert.All(saves, static task => Assert.False(task.IsCompleted));

        store.Release();
        SmtpResponse[] responses = await Task.WhenAll(saves);

        Assert.All(responses, static response => Assert.Equal(SmtpResponse.Ok.ReplyCode, response.ReplyCode));
        Assert.Equal(maxConcurrentProcessing, store.MaxConcurrentCalls);
    }

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 18, 11, 30, 0, TimeSpan.Zero);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    private static SmtpInboundMessageStore CreateMessageStore(
        IInboundMailStore store,
        MailInboxSmtpOptions? options = null) =>
        new(
            store,
            Microsoft.Extensions.Options.Options.Create(options ?? new MailInboxSmtpOptions()),
            FixedTime,
            NullLogger<SmtpInboundMessageStore>.Instance);

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

    private static byte[] CreateBinaryRawMime() {
        var body = new MimePart("application", "octet-stream") {
            Content = new MimeContent(new MemoryStream([0, 1, 127, 128, 255]), ContentEncoding.Default),
            ContentTransferEncoding = ContentEncoding.Binary,
        };
        return CreateRawMimeBytes(body);
    }

    private static byte[] CreateMultipartRawMime(int partCount) {
        var body = new Multipart("mixed");
        for (int i = 0; i < partCount; i++) {
            body.Add(new TextPart("plain") { Text = FormattableString.Invariant($"part-{i}") });
        }

        return CreateRawMimeBytes(body);
    }

    private static byte[] CreateNearLimitBinaryRawMime(int totalBytes) {
        const string headers = "From: sender@example.com\r\n" +
                               "To: admin@fooddiary.club\r\n" +
                               "Subject: near-limit\r\n" +
                               "MIME-Version: 1.0\r\n" +
                               "Content-Type: application/octet-stream\r\n" +
                               "Content-Transfer-Encoding: binary\r\n\r\n";
        byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
        byte[] rawBytes = new byte[totalBytes];
        headerBytes.CopyTo(rawBytes, 0);
        rawBytes.AsSpan(headerBytes.Length).Fill((byte)'x');
        return rawBytes;
    }

    private static byte[] CreateRawMimeBytes(MimeEntity body) {
        var message = new MimeMessage();
        message.MessageId = "message-id";
        message.From.Add(MailboxAddress.Parse("sender@example.com"));
        message.To.Add(MailboxAddress.Parse("admin@fooddiary.club"));
        message.Subject = "Hello";
        message.Body = body;
        using var stream = new MemoryStream();
        message.WriteTo(stream);
        return stream.ToArray();
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

        public Task<InboundMailSaveResult> SaveAsync(InboundMailMessage message, CancellationToken cancellationToken) {
            LastSaved = message;
            return Task.FromResult(new InboundMailSaveResult(Guid.NewGuid(), WasDuplicate: false));
        }

        public Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(
            int limit,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> MarkAsReadAsync(Guid id, DateTimeOffset readAtUtc, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InboundMailRetentionResult> PurgeExpiredAsync(
            DateTimeOffset contentCutoffUtc,
            DateTimeOffset metadataCutoffUtc,
            int batchSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class BlockingInboundMailStore(int expectedConcurrentCalls) : IInboundMailStore {
        private readonly TaskCompletionSource _expectedConcurrencyReached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeCalls;
        private int _maxConcurrentCalls;

        public int ActiveCalls => Volatile.Read(ref _activeCalls);

        public int MaxConcurrentCalls => Volatile.Read(ref _maxConcurrentCalls);

        public Task WaitUntilExpectedConcurrencyAsync() => _expectedConcurrencyReached.Task;

        public void Release() => _release.TrySetResult();

        public async Task<InboundMailSaveResult> SaveAsync(
            InboundMailMessage message,
            CancellationToken cancellationToken) {
            int activeCalls = Interlocked.Increment(ref _activeCalls);
            UpdateMaximum(activeCalls);
            if (activeCalls >= expectedConcurrentCalls) {
                _expectedConcurrencyReached.TrySetResult();
            }

            try {
                await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                return new InboundMailSaveResult(message.Id.Value, WasDuplicate: false);
            } finally {
                Interlocked.Decrement(ref _activeCalls);
            }
        }

        public Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(
            int limit,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InboundMailMessageDetails?> GetMessageDetailsAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> MarkAsReadAsync(
            Guid id,
            DateTimeOffset readAtUtc,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InboundMailRetentionResult> PurgeExpiredAsync(
            DateTimeOffset contentCutoffUtc,
            DateTimeOffset metadataCutoffUtc,
            int batchSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        private void UpdateMaximum(int value) {
            int current = Volatile.Read(ref _maxConcurrentCalls);
            while (value > current) {
                int observed = Interlocked.CompareExchange(ref _maxConcurrentCalls, value, current);
                if (observed == current) {
                    return;
                }

                current = observed;
            }
        }
    }
}
