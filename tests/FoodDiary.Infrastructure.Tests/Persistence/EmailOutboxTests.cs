using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Infrastructure.Persistence.Images;
using FoodDiary.Infrastructure.Persistence.Notifications;
using FoodDiary.Infrastructure.Persistence.Achievements;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

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
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            new FixedDateTimeProvider(Now),
            NullLogger<EmailOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 0, CancellationToken.None);

        Assert.Equal(0, processed);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenSendSucceeds_MarksMessageProcessed() {
        await using FoodDiaryDbContext context = CreateContext();
        var queuedMessage = EmailOutboxMessage.Create(
            CreateEmailMessage(
                toAddresses: ["account-owner@example.com"],
                subject: "Reset your account",
                htmlBody: "<p>Temporary password: temporary-secret; token: reset-token</p>",
                textBody: "Temporary password: temporary-secret; token: reset-token"),
            Now.AddMinutes(-1));
        context.EmailOutbox.Add(queuedMessage);
        await context.SaveChangesAsync();
        var transport = new RecordingEmailTransport();
        var processor = new EmailOutboxProcessor(
            context,
            transport,
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            new FixedDateTimeProvider(Now),
            NullLogger<EmailOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        EmailOutboxMessage message = Assert.Single(context.EmailOutbox);
        Assert.Multiple(
            () => Assert.Equal(1, processed),
            () => Assert.Equal("Reset your account", Assert.Single(transport.Messages).Subject),
            () => Assert.Equal(["account-owner@example.com"], transport.Messages[0].ToAddresses),
            () => Assert.Contains("temporary-secret", transport.Messages[0].HtmlBody, StringComparison.Ordinal),
            () => Assert.Equal($"fooddiary-email-outbox:{queuedMessage.Id:N}", transport.Messages[0].IdempotencyKey),
            () => Assert.NotNull(message.ProcessedOnUtc),
            () => Assert.Equal("[]", message.ToAddressesJson),
            () => Assert.Empty(message.Subject),
            () => Assert.Empty(message.HtmlBody),
            () => Assert.Null(message.TextBody),
            () => Assert.Null(message.LockedUntilUtc),
            () => Assert.Null(message.LockedBy),
            () => Assert.Null(message.LastError));
    }

    [Fact]
    public async Task ProcessDueAsync_WhenCallerCancelsDuringDispatch_RethrowsWithoutRecordingFailure() {
        await using FoodDiaryDbContext context = CreateContext();
        context.EmailOutbox.Add(EmailOutboxMessage.Create(CreateEmailMessage(), Now.AddMinutes(-1)));
        await context.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        EmailOutboxProcessor processor = CreateProcessor(context, new CancelingEmailTransport(cancellation));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            processor.ProcessDueAsync(batchSize: 1, cancellation.Token));

        context.ChangeTracker.Clear();
        EmailOutboxMessage stored = Assert.Single(context.EmailOutbox);
        Assert.Multiple(
            () => Assert.Equal(0, stored.AttemptCount),
            () => Assert.Null(stored.ProcessedOnUtc),
            () => Assert.Null(stored.LastError));
    }

    [Fact]
    public async Task ProcessDueAsync_WhenCallerCancelsAfterDispatch_FinalizesWithServerOwnedToken() {
        await using FoodDiaryDbContext context = CreateContext();
        context.EmailOutbox.Add(EmailOutboxMessage.Create(CreateEmailMessage(), Now.AddMinutes(-1)));
        await context.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        EmailOutboxProcessor processor = CreateProcessor(context, new CancelingSuccessfulEmailTransport(cancellation));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            processor.ProcessDueAsync(batchSize: 1, cancellation.Token));

        context.ChangeTracker.Clear();
        EmailOutboxMessage stored = Assert.Single(context.EmailOutbox);
        Assert.NotNull(stored.ProcessedOnUtc);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenDispatchExceedsBudget_SchedulesSafeTimeoutRetry() {
        await using FoodDiaryDbContext context = CreateContext();
        context.EmailOutbox.Add(EmailOutboxMessage.Create(CreateEmailMessage(), Now.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var options = new OutboxProcessingOptions {
            LeaseDuration = TimeSpan.FromSeconds(17),
            DispatchTimeout = TimeSpan.FromMilliseconds(20),
            FinalizationTimeout = TimeSpan.FromSeconds(1),
        };
        EmailOutboxProcessor processor = CreateProcessor(context, new WaitingEmailTransport(), options);

        int processed = await processor.ProcessDueAsync(batchSize: 1, CancellationToken.None);

        EmailOutboxMessage stored = Assert.Single(context.EmailOutbox);
        Assert.Multiple(
            () => Assert.Equal(0, processed),
            () => Assert.Equal(1, stored.AttemptCount),
            () => Assert.Equal("Outbox dispatch failed (TimeoutException).", stored.LastError));
    }

    [Fact]
    public async Task ProcessDueAsync_WhenSecondFinalizationFails_PreservesFirstMessageProgress() {
        var databaseRoot = new InMemoryDatabaseRoot();
        string databaseName = Guid.NewGuid().ToString("N");
        await using (FoodDiaryDbContext setupContext = CreateContext(databaseName, databaseRoot)) {
            setupContext.EmailOutbox.AddRange(
                EmailOutboxMessage.Create(CreateEmailMessage(subject: "First"), Now.AddMinutes(-2)),
                EmailOutboxMessage.Create(CreateEmailMessage(subject: "Second"), Now.AddMinutes(-1)));
            await setupContext.SaveChangesAsync();
        }

        var transport = new RecordingEmailTransport();
        await using (FoodDiaryDbContext processingContext = CreateContext(
                         databaseName,
                         databaseRoot,
                         new FailOnSaveNumberInterceptor(failureNumber: 2))) {
            EmailOutboxProcessor processor = CreateProcessor(processingContext, transport);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                processor.ProcessDueAsync(batchSize: 2, CancellationToken.None));
        }

        await using FoodDiaryDbContext verificationContext = CreateContext(databaseName, databaseRoot);
        List<EmailOutboxMessage> stored = await verificationContext.EmailOutbox
            .AsNoTracking()
            .OrderBy(static message => message.CreatedOnUtc)
            .ToListAsync();
        Assert.Multiple(
            () => Assert.Equal(["First", "Second"], transport.Messages.Select(static message => message.Subject), StringComparer.Ordinal),
            () => Assert.NotNull(stored[0].ProcessedOnUtc),
            () => Assert.Null(stored[1].ProcessedOnUtc));
    }

    [Fact]
    public async Task ProcessDueAsync_WhenSendFails_SchedulesRetry() {
        await using FoodDiaryDbContext context = CreateContext();
        context.EmailOutbox.Add(EmailOutboxMessage.Create(CreateEmailMessage(), Now.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var processor = new EmailOutboxProcessor(
            context,
            new ThrowingEmailTransport(),
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
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
            () => Assert.Equal("Outbox dispatch failed (InvalidOperationException).", message.LastError));
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
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            new FixedDateTimeProvider(Now),
            NullLogger<EmailOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        EmailOutboxMessage stored = Assert.Single(context.EmailOutbox);
        var replayableMessage = stored.ToEmailMessage();
        Assert.Multiple(
            () => Assert.Equal(0, processed),
            () => Assert.Equal(OutboxProcessingPolicy.MaxAttemptCount, stored.AttemptCount),
            () => Assert.NotNull(stored.DeadLetteredOnUtc),
            () => Assert.Null(stored.LockedUntilUtc),
            () => Assert.Null(stored.LockedBy),
            () => Assert.Equal("Outbox dispatch failed (InvalidOperationException).", stored.LastError),
            () => Assert.Equal(["recipient@example.com"], replayableMessage.ToAddresses),
            () => Assert.Equal("Hello", replayableMessage.Subject),
            () => Assert.Equal("<p>Hello</p>", replayableMessage.HtmlBody),
            () => Assert.Equal("Hello", replayableMessage.TextBody));
    }

    [Fact]
    public async Task ReplayAsync_WhenMessageIsDeadLettered_RequeuesAndAuditsOperatorIntent() {
        await using FoodDiaryDbContext context = CreateContext();
        var message = EmailOutboxMessage.Create(CreateEmailMessage(), Now.AddMinutes(-1));
        message.MarkDeadLettered("provider rejected request", Now.AddSeconds(-1));
        context.EmailOutbox.Add(message);
        await context.SaveChangesAsync();
        var replayService = new OutboxDeadLetterReplayService(context, new FixedDateTimeProvider(Now));

        await replayService.ReplayAsync(
            "email",
            message.Id,
            "ops@example.com",
            "Provider incident resolved",
            expectedAttemptCount: 1,
            CancellationToken.None);

        OutboxReplayAudit audit = Assert.Single(context.OutboxReplayAudits);
        Assert.Multiple(
            () => Assert.Null(message.DeadLetteredOnUtc),
            () => Assert.Equal(Now, message.NextAttemptOnUtc),
            () => Assert.Null(message.LastError),
            () => Assert.Equal("ops@example.com", audit.RequestedBy),
            () => Assert.Equal("Provider incident resolved", audit.Reason),
            () => Assert.Equal("provider rejected request", audit.PreviousError));
    }

    [Fact]
    public async Task ReplayTooling_ListsPreviewsAndFiltersAuditHistory() {
        await using FoodDiaryDbContext context = CreateContext();
        var message = EmailOutboxMessage.Create(CreateEmailMessage(subject: "Dead letter preview"), Now.AddMinutes(-2));
        message.MarkDeadLettered("provider unavailable", Now.AddMinutes(-1));
        context.EmailOutbox.Add(message);
        await context.SaveChangesAsync();
        var replayService = new OutboxDeadLetterReplayService(context, new FixedDateTimeProvider(Now));

        IReadOnlyList<OutboxDeadLetterMessageModel> listed = await replayService.ListDeadLettersAsync(
            "email",
            limit: 10,
            CancellationToken.None);
        OutboxDeadLetterMessageModel? preview = await replayService.GetDeadLetterAsync(
            "email",
            message.Id,
            CancellationToken.None);
        OutboxReplayAuditModel audit = await replayService.ReplayAsync(
            "email",
            message.Id,
            "operator@example.com",
            "Transient provider outage resolved",
            expectedAttemptCount: 1,
            CancellationToken.None);
        IReadOnlyList<OutboxReplayAuditModel> history = await replayService.ListReplayHistoryAsync(
            "email",
            message.Id,
            limit: 10,
            CancellationToken.None);

        OutboxDeadLetterMessageModel listedMessage = Assert.Single(listed);
        OutboxReplayAuditModel storedAudit = Assert.Single(history);
        Assert.NotNull(preview);
        Assert.Multiple(
            () => Assert.Equal("Dead letter preview", listedMessage.Summary),
            () => Assert.Equal("provider unavailable", preview.LastError),
            () => Assert.Equal(audit.Id, storedAudit.Id),
            () => Assert.Equal("operator@example.com", storedAudit.RequestedBy));
    }

    [Fact]
    public async Task ReplayAsync_WhenAttemptCountChangedAfterInspection_RejectsStaleOperatorAction() {
        await using FoodDiaryDbContext context = CreateContext();
        var message = EmailOutboxMessage.Create(CreateEmailMessage(), Now.AddMinutes(-1));
        message.MarkDeadLettered("provider unavailable", Now);
        context.EmailOutbox.Add(message);
        await context.SaveChangesAsync();
        var replayService = new OutboxDeadLetterReplayService(context, new FixedDateTimeProvider(Now));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            replayService.ReplayAsync(
                "email",
                message.Id,
                "operator@example.com",
                "Retry",
                expectedAttemptCount: 2,
                CancellationToken.None));

        Assert.Contains("Expected 2 attempts, observed 1", exception.Message, StringComparison.Ordinal);
        Assert.NotNull(message.DeadLetteredOnUtc);
        Assert.Empty(context.OutboxReplayAudits);
    }

    [Fact]
    public async Task ReplayTooling_ListsAndPreviewsImageAndWebPushDeadLetters() {
        await using FoodDiaryDbContext context = CreateContext();
        var image = ImageObjectDeletionOutboxMessage.Create("users/test/dead.webp", Now.AddMinutes(-3));
        image.MarkDeadLettered("image failure", Now.AddMinutes(-2));
        var webPush = NotificationWebPushOutboxMessage.Create(NotificationId.New(), Now.AddMinutes(-2));
        webPush.MarkDeadLettered("push failure", Now.AddMinutes(-1));
        context.ImageObjectDeletionOutbox.Add(image);
        context.NotificationWebPushOutbox.Add(webPush);
        await context.SaveChangesAsync();
        var service = new OutboxDeadLetterReplayService(context, new FixedDateTimeProvider(Now));

        IReadOnlyList<OutboxDeadLetterMessageModel> all = await service.ListDeadLettersAsync(
            outboxName: null,
            limit: 10,
            CancellationToken.None);
        OutboxDeadLetterMessageModel? imagePreview = await service.GetDeadLetterAsync("image_object_deletion", image.Id, CancellationToken.None);
        OutboxDeadLetterMessageModel? pushPreview = await service.GetDeadLetterAsync("notification_web_push", webPush.Id, CancellationToken.None);

        Assert.NotNull(imagePreview);
        Assert.NotNull(pushPreview);
        Assert.Multiple(
            () => Assert.Equal(2, all.Count),
            () => Assert.Equal(image.ObjectKey, imagePreview.Summary),
            () => Assert.Equal(webPush.NotificationId.Value.ToString(), pushPreview.Summary));
    }

    [Fact]
    public async Task ReplayTooling_PreviewsAndReplaysAchievementEvaluationDeadLetter() {
        await using FoodDiaryDbContext context = CreateContext();
        var message = AchievementEvaluationOutboxMessage.Create(UserId.New(), Now.AddMinutes(-2));
        message.MarkDeadLettered("achievement failure", Now.AddMinutes(-1));
        context.AchievementEvaluationOutbox.Add(message);
        await context.SaveChangesAsync();
        var service = new OutboxDeadLetterReplayService(context, new FixedDateTimeProvider(Now));

        OutboxDeadLetterMessageModel? preview = await service.GetDeadLetterAsync(
            "achievement_evaluation",
            message.Id,
            CancellationToken.None);
        OutboxReplayAuditModel audit = await service.ReplayAsync(
            "achievement_evaluation",
            message.Id,
            "operator@example.com",
            "Reconciliation recovered",
            expectedAttemptCount: 1,
            CancellationToken.None);

        Assert.NotNull(preview);
        Assert.Multiple(
            () => Assert.Equal(message.UserId.Value.ToString(), preview.Summary),
            () => Assert.Equal("achievement failure", preview.LastError),
            () => Assert.Equal("achievement failure", audit.PreviousError),
            () => Assert.Null(message.DeadLetteredOnUtc));
    }

    [Fact]
    public async Task ReplayAsync_WithInvalidArgumentsOrMessageState_Throws() {
        await using FoodDiaryDbContext context = CreateContext();
        var active = EmailOutboxMessage.Create(CreateEmailMessage(), Now);
        context.EmailOutbox.Add(active);
        await context.SaveChangesAsync();
        var service = new OutboxDeadLetterReplayService(context, new FixedDateTimeProvider(Now));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.ReplayAsync(
            "email", active.Id, "operator", "reason", expectedAttemptCount: 0, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReplayAsync(
            "email", active.Id, "operator", "reason", expectedAttemptCount: 1, CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task ListDeadLettersAsync_WithInvalidLimit_Throws(int limit) {
        await using FoodDiaryDbContext context = CreateContext();
        var service = new OutboxDeadLetterReplayService(context, new FixedDateTimeProvider(Now));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ListDeadLettersAsync(outboxName: null, limit, CancellationToken.None));
    }

    [Fact]
    public async Task GetDeadLetterAsync_WithEmptyMessageId_Throws() {
        await using FoodDiaryDbContext context = CreateContext();
        var service = new OutboxDeadLetterReplayService(context, new FixedDateTimeProvider(Now));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetDeadLetterAsync("email", Guid.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task DefensiveOutboxSwitchFallbacks_RejectUnsupportedMessageTypes() {
        await using FoodDiaryDbContext context = CreateContext();
        var service = new OutboxDeadLetterReplayService(context, new FixedDateTimeProvider(Now));
        MethodInfo findAsync = typeof(OutboxDeadLetterReplayService).GetMethod(
            "FindAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var findTask = (Task<IOutboxMessage?>)findAsync.Invoke(
            service,
            ["unsupported", Guid.NewGuid(), false, CancellationToken.None])!;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => findTask);

        var unsupported = new UnsupportedOutboxMessage();
        MethodInfo toModel = typeof(OutboxDeadLetterReplayService).GetMethod(
            "ToModel",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            [typeof(string), typeof(IOutboxMessage)],
            modifiers: null)!;
        MethodInfo getLastError = typeof(OutboxDeadLetterReplayService).GetMethod(
            "GetLastError",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        TargetInvocationException modelException = Assert.Throws<TargetInvocationException>(() =>
            toModel.Invoke(obj: null, ["unsupported", unsupported]));
        Assert.IsType<ArgumentOutOfRangeException>(modelException.InnerException);
        Assert.Null(getLastError.Invoke(obj: null, [unsupported]));
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

        OutboxClaimBatch<EmailOutboxMessage> claim = await OutboxMessageClaimer.ClaimDueAsync(
            context,
            context.EmailOutbox,
            "\"EmailOutbox\"",
            batchSize: 2,
            Now,
            TimeSpan.FromMinutes(2),
            cancellationToken: CancellationToken.None);

        Assert.Equal(["Expired lock", "First due"], claim.Messages.Select(static message => message.Subject), StringComparer.Ordinal);
        EmailOutboxMessage claimedMessage = claim.Messages[0];
        Assert.Multiple(
            () => Assert.Equal(1, claim.ReclaimedCount),
            () => Assert.NotNull(claimedMessage.LockedUntilUtc),
            () => Assert.Equal(Now.AddMinutes(2), claimedMessage.LockedUntilUtc),
            () => Assert.NotNull(claimedMessage.LockedBy),
            () => Assert.True(claimedMessage.LockedBy?.Length <= 128));
    }

    private static EmailMessage CreateEmailMessage(
        string fromAddress = "sender@example.com",
        string fromName = "Sender",
        IReadOnlyList<string>? toAddresses = null,
        string subject = "Hello",
        string htmlBody = "<p>Hello</p>",
        string? textBody = "Hello") =>
        new(
            fromAddress,
            fromName,
            toAddresses ?? ["recipient@example.com"],
            subject,
            htmlBody,
            textBody);

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }

    private static FoodDiaryDbContext CreateContext(
        string databaseName,
        InMemoryDatabaseRoot databaseRoot,
        SaveChangesInterceptor? interceptor = null) {
        DbContextOptionsBuilder<FoodDiaryDbContext> optionsBuilder = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot);
        if (interceptor is not null) {
            optionsBuilder.AddInterceptors(interceptor);
        }

        return new FoodDiaryDbContext(optionsBuilder.Options);
    }

    private static EmailOutboxProcessor CreateProcessor(
        FoodDiaryDbContext context,
        IEmailTransport transport,
        OutboxProcessingOptions? options = null) =>
        new(
            context,
            transport,
            Microsoft.Extensions.Options.Options.Create(options ?? new OutboxProcessingOptions()),
            new FixedDateTimeProvider(Now),
            NullLogger<EmailOutboxProcessor>.Instance);

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

    [ExcludeFromCodeCoverage]
    private sealed class CancelingEmailTransport(CancellationTokenSource cancellation) : IEmailTransport {
        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken) {
            await cancellation.CancelAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CancelingSuccessfulEmailTransport(CancellationTokenSource cancellation) : IEmailTransport {
        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken) {
            await cancellation.CancelAsync();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class WaitingEmailTransport : IEmailTransport {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
            Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailOnSaveNumberInterceptor(int failureNumber) : SaveChangesInterceptor {
        private int _saveCount;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) {
            if (Interlocked.Increment(ref _saveCount) == failureNumber) {
                throw new InvalidOperationException("Simulated finalization failure.");
            }

            return ValueTask.FromResult(result);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class UnsupportedOutboxMessage : IOutboxMessage {
        public Guid Id => Guid.Empty;
        public DateTime CreatedOnUtc => Now;
        public int AttemptCount => 1;
        public DateTime? ProcessedOnUtc => null;
        public DateTime? DeadLetteredOnUtc => Now;
        public DateTime? LockedUntilUtc => null;
        public string? LockedBy => null;

        public void MarkClaimed(DateTime lockedUntilUtc, string lockedBy) { }
        public void MarkProcessed(DateTime processedOnUtc) { }
        public void MarkDeadLettered(string error, DateTime deadLetteredOnUtc) { }
        public void MarkFailed(string error, DateTime nextAttemptOnUtc) { }
        public void MarkReplayed(DateTime nextAttemptOnUtc) { }
    }
}
