using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class EmailOutboxTests {
    private static readonly DateTime Now = new(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task EnqueueAsync_PersistsDueMessage() {
        await using FoodDiaryDbContext context = CreateContext();
        var outbox = new EmailOutbox(context, new FixedDateTimeProvider(Now));

        await outbox.EnqueueAsync(CreateEmailMessage(), CancellationToken.None);
        await context.SaveChangesAsync();

        EmailOutboxMessage message = Assert.Single(context.EmailOutbox);
        Assert.Equal("sender@example.com", message.FromAddress);
        Assert.Equal(Now, message.CreatedOnUtc);
        Assert.Equal(Now, message.NextAttemptOnUtc);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenBatchSizeIsNotPositive_ReturnsZero() {
        await using FoodDiaryDbContext context = CreateContext();
        var processor = new EmailOutboxProcessor(
            context,
            new RecordingEmailTransport(),
            new FixedDateTimeProvider(Now),
            NullLogger<EmailOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 0, CancellationToken.None);

        Assert.Equal(0, processed);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenSendSucceeds_MarksMessageProcessed() {
        await using FoodDiaryDbContext context = CreateContext();
        context.EmailOutbox.Add(EmailOutboxMessage.Create(CreateEmailMessage(), Now.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var transport = new RecordingEmailTransport();
        var processor = new EmailOutboxProcessor(
            context,
            transport,
            new FixedDateTimeProvider(Now),
            NullLogger<EmailOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        EmailOutboxMessage message = Assert.Single(context.EmailOutbox);
        Assert.Multiple(
            () => Assert.Equal(1, processed),
            () => Assert.Equal("Hello", Assert.Single(transport.Messages).Subject),
            () => Assert.NotNull(message.ProcessedOnUtc),
            () => Assert.Null(message.LockedUntilUtc),
            () => Assert.Null(message.LockedBy),
            () => Assert.Null(message.LastError));
    }

    [Fact]
    public async Task ProcessDueAsync_WhenSendFails_SchedulesRetry() {
        await using FoodDiaryDbContext context = CreateContext();
        context.EmailOutbox.Add(EmailOutboxMessage.Create(CreateEmailMessage(), Now.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var processor = new EmailOutboxProcessor(
            context,
            new ThrowingEmailTransport(),
            new FixedDateTimeProvider(Now),
            NullLogger<EmailOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        EmailOutboxMessage message = Assert.Single(context.EmailOutbox);
        Assert.Multiple(
            () => Assert.Equal(0, processed),
            () => Assert.Null(message.ProcessedOnUtc),
            () => Assert.Null(message.DeadLetteredOnUtc),
            () => Assert.Equal(1, message.AttemptCount),
            () => Assert.True(message.NextAttemptOnUtc > Now),
            () => Assert.Contains("Simulated", message.LastError, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProcessDueAsync_WhenMaxAttemptReached_DeadLettersMessage() {
        await using FoodDiaryDbContext context = CreateContext();
        var message = EmailOutboxMessage.Create(CreateEmailMessage(), Now.AddMinutes(-1));
        for (int i = 0; i < OutboxProcessingPolicy.MaxAttemptCount - 1; i++) {
            message.MarkFailed("previous failure", Now.AddMinutes(-1));
        }

        context.EmailOutbox.Add(message);
        await context.SaveChangesAsync();
        var processor = new EmailOutboxProcessor(
            context,
            new ThrowingEmailTransport(),
            new FixedDateTimeProvider(Now),
            NullLogger<EmailOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        EmailOutboxMessage stored = Assert.Single(context.EmailOutbox);
        Assert.Multiple(
            () => Assert.Equal(0, processed),
            () => Assert.Equal(OutboxProcessingPolicy.MaxAttemptCount, stored.AttemptCount),
            () => Assert.NotNull(stored.DeadLetteredOnUtc),
            () => Assert.Null(stored.LockedUntilUtc),
            () => Assert.Null(stored.LockedBy),
            () => Assert.Contains("Simulated", stored.LastError, StringComparison.Ordinal));
    }

    [Fact]
    public void Create_WithInvalidRequiredFields_Throws() {
        Assert.Multiple(
            () => Assert.Equal("message", Assert.Throws<ArgumentException>(() =>
                EmailOutboxMessage.Create(CreateEmailMessage(fromAddress: " "), Now)).ParamName),
            () => Assert.Equal("message", Assert.Throws<ArgumentException>(() =>
                EmailOutboxMessage.Create(CreateEmailMessage(toAddresses: []), Now)).ParamName),
            () => Assert.Equal("message", Assert.Throws<ArgumentException>(() =>
                EmailOutboxMessage.Create(CreateEmailMessage(toAddresses: ["recipient@example.com", " "]), Now)).ParamName),
            () => Assert.Equal("message", Assert.Throws<ArgumentException>(() =>
                EmailOutboxMessage.Create(CreateEmailMessage(subject: " "), Now)).ParamName));
    }

    [Fact]
    public void MessageLifecycle_NormalizesTrimsSerializesAndTruncatesValues() {
        var localDate = new DateTime(2026, 7, 8, 13, 0, 0, DateTimeKind.Local);
        var message = EmailOutboxMessage.Create(
            CreateEmailMessage(
                fromAddress: " sender@example.com ",
                fromName: " Sender ",
                toAddresses: [" first@example.com ", " second@example.com "]),
            localDate);

        var email = message.ToEmailMessage();
        message.MarkClaimed(Now.AddMinutes(5), new string('w', 140));
        message.MarkProcessed(Now.AddMinutes(1));
        message.MarkDeadLettered(" ", Now.AddMinutes(2));
        message.MarkFailed($"  {new string('x', 2100)}  ", Now.AddMinutes(3));

        Assert.Multiple(
            () => Assert.Equal("sender@example.com", email.FromAddress),
            () => Assert.Equal("Sender", email.FromName),
            () => Assert.Equal(["first@example.com", "second@example.com"], email.ToAddresses),
            () => Assert.Equal(DateTimeKind.Utc, message.CreatedOnUtc.Kind),
            () => Assert.Equal(2, message.AttemptCount),
            () => Assert.Equal(2048, message.LastError?.Length),
            () => Assert.Null(message.LockedUntilUtc),
            () => Assert.Null(message.LockedBy));
    }

    [Fact]
    public async Task OutboxMessageClaimer_WithInMemoryProvider_ClaimsOnlyDueUnlockedMessagesInOrder() {
        await using FoodDiaryDbContext context = CreateContext();
        var locked = EmailOutboxMessage.Create(CreateEmailMessage(subject: "Locked"), Now.AddMinutes(-4));
        locked.MarkClaimed(Now.AddMinutes(1), "worker");
        var expiredLock = EmailOutboxMessage.Create(CreateEmailMessage(subject: "Expired lock"), Now.AddMinutes(-5));
        expiredLock.MarkClaimed(Now.AddMinutes(-1), "old-worker");
        var processed = EmailOutboxMessage.Create(CreateEmailMessage(subject: "Processed"), Now.AddMinutes(-3));
        processed.MarkProcessed(Now);
        var future = EmailOutboxMessage.Create(CreateEmailMessage(subject: "Future"), Now.AddMinutes(1));
        var firstDue = EmailOutboxMessage.Create(CreateEmailMessage(subject: "First due"), Now.AddMinutes(-2));
        var secondDue = EmailOutboxMessage.Create(CreateEmailMessage(subject: "Second due"), Now.AddMinutes(-1));
        context.EmailOutbox.AddRange(locked, expiredLock, processed, future, secondDue, firstDue);
        await context.SaveChangesAsync();

        List<EmailOutboxMessage> claimed = await OutboxMessageClaimer.ClaimDueAsync(
            context,
            context.EmailOutbox,
            "\"EmailOutbox\"",
            batchSize: 2,
            Now,
            cancellationToken: CancellationToken.None);

        Assert.Equal(["Expired lock", "First due"], claimed.Select(static message => message.Subject));
        EmailOutboxMessage claimedMessage = claimed[0];
        Assert.Multiple(
            () => Assert.NotNull(claimedMessage.LockedUntilUtc),
            () => Assert.NotNull(claimedMessage.LockedBy),
            () => Assert.True(claimedMessage.LockedBy?.Length <= 128));
    }

    private static EmailMessage CreateEmailMessage(
        string fromAddress = "sender@example.com",
        string fromName = "Sender",
        IReadOnlyList<string>? toAddresses = null,
        string subject = "Hello") =>
        new(
            fromAddress,
            fromName,
            toAddresses ?? ["recipient@example.com"],
            subject,
            "<p>Hello</p>",
            "Hello");

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingEmailTransport : IEmailTransport {
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingEmailTransport : IEmailTransport {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("Simulated email transport failure."));
    }
}
