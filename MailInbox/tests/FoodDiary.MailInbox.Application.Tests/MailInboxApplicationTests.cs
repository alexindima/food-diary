using FluentValidation;
using FluentValidation.Results;
using System.Diagnostics.Metrics;
using System.Globalization;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Common.Behaviors;
using FoodDiary.Results;
using FoodDiary.MailInbox.Application.Common.Results;
using FoodDiary.MailInbox.Application.Health;
using FoodDiary.MailInbox.Application.Messages.Commands.MarkInboundMailMessageRead;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessageDetails;
using FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessages;
using FoodDiary.MailInbox.Application.Telemetry;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.MailInbox.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxApplicationTests {
    [Fact]
    public async Task GetInboundMailMessagesHandler_ForwardsLimitAndCancellationToken() {
        using var cts = new CancellationTokenSource();
        var expected = new InboundMailMessageSummary(
            Guid.NewGuid(),
            "sender@example.com",
            ["admin@fooddiary.club"],
            "Hello",
            InboundMailMessageCategories.General,
            InboundMailMessageStatus.Received.ToString(),
            ReadAtUtc: null,
            DateTimeOffset.UtcNow);
        var store = new RecordingInboundMailStore {
            MessageSummaries = [expected],
        };
        var handler = new GetInboundMailMessagesQueryHandler(store);

        Result<IReadOnlyList<InboundMailMessageSummary>> result = await handler.Handle(new GetInboundMailMessagesQuery(25), cts.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, Assert.Single(result.Value));
        Assert.Equal(25, store.LastMessagesLimit);
        Assert.Equal(cts.Token, store.LastMessagesCancellationToken);
    }

    [Fact]
    public async Task GetInboundMailMessageDetailsHandler_WhenMissing_ReturnsNotFound() {
        var id = Guid.NewGuid();
        var handler = new GetInboundMailMessageDetailsQueryHandler(new RecordingInboundMailStore());

        Result<InboundMailMessageDetails> result = await handler.Handle(new GetInboundMailMessageDetailsQuery(id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("MailInbox.Message.NotFound", result.Error?.Code);
        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
    }

    [Fact]
    public async Task CheckMailInboxReadinessHandler_ForwardsCancellationToken() {
        using var cts = new CancellationTokenSource();
        var checker = new RecordingReadinessChecker();
        var handler = new CheckMailInboxReadinessQueryHandler(checker);

        Result result = await handler.Handle(new CheckMailInboxReadinessQuery(), cts.Token);

        Assert.True(result.IsSuccess);
        Assert.True(checker.Called);
        Assert.Equal(cts.Token, checker.CancellationToken);
    }

    [Fact]
    public async Task GetInboundMailMessageDetailsHandler_WhenFound_ReturnsDetails() {
        var id = Guid.NewGuid();
        InboundMailMessageDetails details = CreateDetails(id);
        var handler = new GetInboundMailMessageDetailsQueryHandler(new RecordingInboundMailStore {
            Details = details,
        });

        Result<InboundMailMessageDetails> result = await handler.Handle(new GetInboundMailMessageDetailsQuery(id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(details, result.Value);
    }

    [Fact]
    public async Task MarkInboundMailMessageReadHandler_WhenFound_MarksMessageRead() {
        using var cts = new CancellationTokenSource();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-06-14T10:00:00Z", CultureInfo.InvariantCulture);
        var store = new RecordingInboundMailStore {
            Details = CreateDetails(id),
        };
        var handler = new MarkInboundMailMessageReadCommandHandler(store, new FixedTimeProvider(now));

        Result result = await handler.Handle(new MarkInboundMailMessageReadCommand(id), cts.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, store.LastMessageId);
        Assert.Equal(now, store.LastReadAtUtc);
        Assert.Equal(cts.Token, store.LastMessagesCancellationToken);
    }

    [Fact]
    public async Task MarkInboundMailMessageReadHandler_WhenMissing_ReturnsNotFound() {
        var id = Guid.NewGuid();
        var handler = new MarkInboundMailMessageReadCommandHandler(
            new RecordingInboundMailStore(),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        Result result = await handler.Handle(new MarkInboundMailMessageReadCommand(id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("MailInbox.Message.NotFound", result.Error?.Code);
    }

    [Fact]
    public void MessageModels_ExposeConstructedValues() {
        var id = Guid.Parse("1f25ea80-d126-42ec-804c-b793c4d9435e");
        var receivedAt = DateTimeOffset.Parse("2026-06-14T10:00:00Z", CultureInfo.InvariantCulture);
        DateTimeOffset readAt = receivedAt.AddMinutes(1);
        DateTimeOffset purgedAt = receivedAt.AddDays(30);
        string[] recipients = ["admin@fooddiary.club"];
        var dmarcRecord = new DmarcReportRecordPreview(
            "192.0.2.1",
            7,
            "none",
            "pass",
            "pass",
            "fooddiary.club",
            "sender.fooddiary.club",
            "fooddiary.club",
            "pass",
            "fooddiary.club",
            "pass");
        DmarcReportRecordPreview[] dmarcRecords = [dmarcRecord];
        var dmarcReport = new DmarcReportPreview(
            "Example Mail",
            "report-42",
            "fooddiary.club",
            receivedAt.AddDays(-1),
            receivedAt,
            dmarcRecords);
        var details = new InboundMailMessageDetails(
            id,
            "message-id",
            "sender@example.com",
            recipients,
            "Hello",
            "text",
            "<p>text</p>",
            "raw",
            InboundMailMessageCategories.DmarcReport,
            dmarcReport,
            InboundMailMessageStatus.Received.ToString(),
            readAt,
            receivedAt,
            purgedAt);
        var summary = new InboundMailMessageSummary(
            id,
            "sender@example.com",
            recipients,
            "Hello",
            InboundMailMessageCategories.DmarcReport,
            InboundMailMessageStatus.Received.ToString(),
            readAt,
            receivedAt);
        var retention = new InboundMailRetentionResult(3, 2);
        var save = new InboundMailSaveResult(id, WasDuplicate: true);

        Assert.Multiple(
            () => Assert.Equal("192.0.2.1", dmarcRecord.SourceIp),
            () => Assert.Equal(7, dmarcRecord.Count),
            () => Assert.Equal("none", dmarcRecord.Disposition),
            () => Assert.Equal("pass", dmarcRecord.Dkim),
            () => Assert.Equal("pass", dmarcRecord.Spf),
            () => Assert.Equal("fooddiary.club", dmarcRecord.HeaderFrom),
            () => Assert.Equal("sender.fooddiary.club", dmarcRecord.EnvelopeFrom),
            () => Assert.Equal("fooddiary.club", dmarcRecord.DkimDomain),
            () => Assert.Equal("pass", dmarcRecord.DkimResult),
            () => Assert.Equal("fooddiary.club", dmarcRecord.SpfDomain),
            () => Assert.Equal("pass", dmarcRecord.SpfResult),
            () => Assert.Equal("Example Mail", dmarcReport.OrganizationName),
            () => Assert.Equal("report-42", dmarcReport.ReportId),
            () => Assert.Equal("fooddiary.club", dmarcReport.Domain),
            () => Assert.Equal(receivedAt.AddDays(-1), dmarcReport.DateRangeStartUtc),
            () => Assert.Equal(receivedAt, dmarcReport.DateRangeEndUtc),
            () => Assert.Same(dmarcRecords, dmarcReport.Records),
            () => Assert.Equal(id, details.Id),
            () => Assert.Equal("message-id", details.MessageId),
            () => Assert.Equal("sender@example.com", details.FromAddress),
            () => Assert.Same(recipients, details.ToRecipients),
            () => Assert.Equal("Hello", details.Subject),
            () => Assert.Equal("text", details.TextBody),
            () => Assert.Equal("<p>text</p>", details.HtmlBody),
            () => Assert.Equal("raw", details.RawMime),
            () => Assert.Equal(InboundMailMessageCategories.DmarcReport, details.Category),
            () => Assert.Same(dmarcReport, details.DmarcReport),
            () => Assert.Equal(InboundMailMessageStatus.Received.ToString(), details.Status),
            () => Assert.Equal(readAt, details.ReadAtUtc),
            () => Assert.Equal(receivedAt, details.ReceivedAtUtc),
            () => Assert.Equal(purgedAt, details.ContentPurgedAtUtc),
            () => Assert.Equal(id, summary.Id),
            () => Assert.Equal("sender@example.com", summary.FromAddress),
            () => Assert.Same(recipients, summary.ToRecipients),
            () => Assert.Equal("Hello", summary.Subject),
            () => Assert.Equal(InboundMailMessageCategories.DmarcReport, summary.Category),
            () => Assert.Equal(InboundMailMessageStatus.Received.ToString(), summary.Status),
            () => Assert.Equal(readAt, summary.ReadAtUtc),
            () => Assert.Equal(receivedAt, summary.ReceivedAtUtc),
            () => Assert.Equal(3, retention.ContentPurgedCount),
            () => Assert.Equal(2, retention.MetadataDeletedCount),
            () => Assert.Equal(id, save.Id),
            () => Assert.True(save.WasDuplicate));
    }

    [Theory]
    [InlineData(MailInboxIngestionOutcome.Overloaded, "overloaded")]
    [InlineData(MailInboxIngestionOutcome.EmptyMessage, "empty_message")]
    [InlineData(MailInboxIngestionOutcome.MessageTooLarge, "message_too_large")]
    [InlineData(MailInboxIngestionOutcome.IpByteRateLimited, "ip_byte_rate_limited")]
    [InlineData(MailInboxIngestionOutcome.MimePartLimit, "mime_part_limit")]
    [InlineData(MailInboxIngestionOutcome.RecipientLimit, "recipient_limit")]
    [InlineData(MailInboxIngestionOutcome.MetadataLimit, "metadata_limit")]
    [InlineData(MailInboxIngestionOutcome.Duplicate, "duplicate")]
    [InlineData(MailInboxIngestionOutcome.Success, "success")]
    [InlineData(MailInboxIngestionOutcome.Canceled, "canceled")]
    [InlineData(MailInboxIngestionOutcome.StorageQuota, "storage_quota")]
    [InlineData(MailInboxIngestionOutcome.Failure, "failure")]
    public void RecordIngestion_UsesStableBoundedOutcomeTag(
        MailInboxIngestionOutcome outcome,
        string expectedTagValue) {
        IReadOnlyList<string> outcomes = CaptureOutcomes(() =>
            MailInboxTelemetry.RecordIngestion(outcome, TimeSpan.FromMilliseconds(5), 42));

        Assert.Multiple(
            () => Assert.Equal(3, outcomes.Count),
            () => Assert.All(outcomes, value => Assert.Equal(expectedTagValue, value)));
    }

    [Theory]
    [InlineData(MailInboxAdmissionOutcome.MessageTooLarge, "message_too_large")]
    [InlineData(MailInboxAdmissionOutcome.SessionRateLimited, "session_rate_limited")]
    [InlineData(MailInboxAdmissionOutcome.IpRateLimited, "ip_rate_limited")]
    [InlineData(MailInboxAdmissionOutcome.SenderRateLimited, "sender_rate_limited")]
    [InlineData(MailInboxAdmissionOutcome.Accepted, "accepted")]
    [InlineData(MailInboxAdmissionOutcome.RecipientNotAllowed, "recipient_not_allowed")]
    [InlineData(MailInboxAdmissionOutcome.RecipientLimitExceeded, "recipient_limit_exceeded")]
    public void RecordAdmission_UsesStableBoundedOutcomeTag(
        MailInboxAdmissionOutcome outcome,
        string expectedTagValue) {
        IReadOnlyList<string> outcomes = CaptureOutcomes(() => MailInboxTelemetry.RecordAdmission(outcome));

        Assert.Equal([expectedTagValue], outcomes);
    }

    [Theory]
    [InlineData(MailInboxRetentionOutcome.Failure, "failure")]
    [InlineData(MailInboxRetentionOutcome.ContentPurged, "content_purged")]
    [InlineData(MailInboxRetentionOutcome.MetadataDeleted, "metadata_deleted")]
    public void RecordRetention_UsesStableBoundedOutcomeTag(
        MailInboxRetentionOutcome outcome,
        string expectedTagValue) {
        IReadOnlyList<string> outcomes = CaptureOutcomes(() => MailInboxTelemetry.RecordRetention(outcome, 2));

        Assert.Equal([expectedTagValue], outcomes);
    }

    [Fact]
    public void RecordTelemetry_WithUnknownOutcome_Throws() {
        Assert.Multiple(
            () => Assert.Throws<ArgumentOutOfRangeException>(() => MailInboxTelemetry.RecordIngestion(
                (MailInboxIngestionOutcome)(-1),
                TimeSpan.Zero,
                0)),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => MailInboxTelemetry.RecordAdmission(
                (MailInboxAdmissionOutcome)(-1))),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => MailInboxTelemetry.RecordRetention(
                (MailInboxRetentionOutcome)(-1),
                0)));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(200, true)]
    [InlineData(0, false)]
    [InlineData(201, false)]
    public async Task GetInboundMailMessagesValidator_ValidatesLimitRange(int limit, bool expectedValid) {
        var validator = new GetInboundMailMessagesQueryValidator();

        ValidationResult result = await validator.ValidateAsync(new GetInboundMailMessagesQuery(limit));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", false)]
    [InlineData("1f25ea80-d126-42ec-804c-b793c4d9435e", true)]
    public async Task MarkInboundMailMessageReadValidator_ValidatesMessageId(string id, bool expectedValid) {
        var validator = new MarkInboundMailMessageReadCommandValidator();

        ValidationResult result = await validator.ValidateAsync(new MarkInboundMailMessageReadCommand(Guid.Parse(id)));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public async Task MailInboxValidationBehavior_WhenValidationFails_ReturnsTypedFailureAndDoesNotInvokeNext() {
        var behavior = new MailInboxValidationBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>(
            [new GetInboundMailMessagesQueryValidator()]);
        bool nextCalled = false;

        Result<IReadOnlyList<InboundMailMessageSummary>> result = await behavior.Handle(
            new GetInboundMailMessagesQuery(0),
            _ => {
                nextCalled = true;
                return Task.FromResult(Result.Success<IReadOnlyList<InboundMailMessageSummary>>([]));
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(nextCalled);
        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
        Assert.Contains("Limit", result.Error?.Details?.Keys ?? [], StringComparer.Ordinal);
    }

    [Fact]
    public async Task MailInboxValidationBehavior_WhenNonGenericResultValidationFails_ReturnsFailure() {
        var behavior = new MailInboxValidationBehavior<TestCommand, Result>(
            [new AlwaysFailingTestCommandValidator()]);

        Result result = await behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
        Assert.Equal("Validation.Invalid", result.Error?.Code);
    }

    [Fact]
    public async Task MailInboxValidationBehavior_WhenMultipleMessagesExist_UsesGenericValidationMessage() {
        var behavior = new MailInboxValidationBehavior<TestCommand, Result>(
            [new MultipleFailuresTestCommandValidator()]);

        Result result = await behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("One or more validation errors occurred.", result.Error?.Message);
        Assert.NotNull(result.Error?.Details);
        Assert.Equal(["First", "Second"], result.Error!.Details!["Name"]);
    }

    [Fact]
    public async Task MailInboxValidationBehavior_WhenFailureResponseTypeIsUnsupported_Throws() {
        var behavior = new MailInboxValidationBehavior<TestUnsupportedResultCommand, UnsupportedResult>(
            [new AlwaysFailingUnsupportedResultCommandValidator()]);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new TestUnsupportedResultCommand(),
            _ => Task.FromResult(new UnsupportedResult()),
            CancellationToken.None));

        Assert.Contains(nameof(UnsupportedResult), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MailInboxValidationBehavior_WhenNoValidators_InvokesNext() {
        var behavior = new MailInboxValidationBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>([]);
        var response = Result.Success<IReadOnlyList<InboundMailMessageSummary>>([]);

        Result<IReadOnlyList<InboundMailMessageSummary>> result = await behavior.Handle(
            new GetInboundMailMessagesQuery(10),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.Same(response, result);
    }

    [Fact]
    public async Task MailInboxValidationBehavior_WhenValidationSucceeds_InvokesNext() {
        var behavior = new MailInboxValidationBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>(
            [new GetInboundMailMessagesQueryValidator()]);
        var response = Result.Success<IReadOnlyList<InboundMailMessageSummary>>([]);

        Result<IReadOnlyList<InboundMailMessageSummary>> result = await behavior.Handle(
            new GetInboundMailMessagesQuery(10),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.Same(response, result);
    }

    [Fact]
    public async Task MailInboxLoggingBehavior_WhenNextSucceeds_ReturnsResponse() {
        var behavior = new MailInboxLoggingBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>(
            NullLogger<MailInboxLoggingBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>>.Instance);
        var response = Result.Success<IReadOnlyList<InboundMailMessageSummary>>([]);

        Result<IReadOnlyList<InboundMailMessageSummary>> result = await behavior.Handle(
            new GetInboundMailMessagesQuery(10),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.Same(response, result);
    }

    [Fact]
    public async Task MailInboxLoggingBehavior_WhenNextFails_ReturnsFailure() {
        var behavior = new MailInboxLoggingBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>(
            NullLogger<MailInboxLoggingBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>>.Instance);
        var response = Result.Failure<IReadOnlyList<InboundMailMessageSummary>>(MailInboxErrors.MessageNotFound(Guid.NewGuid()));

        Result<IReadOnlyList<InboundMailMessageSummary>> result = await behavior.Handle(
            new GetInboundMailMessagesQuery(10),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.Same(response, result);
    }

    [Fact]
    public async Task MailInboxLoggingBehavior_WhenNextThrows_Rethrows() {
        var behavior = new MailInboxLoggingBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>(
            NullLogger<MailInboxLoggingBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>>.Instance);
        var exception = new InvalidOperationException("boom");

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new GetInboundMailMessagesQuery(10),
            _ => throw exception,
            CancellationToken.None));

        Assert.Same(exception, thrown);
    }

    [Fact]
    public void AddMailInboxApplication_RegistersMediatorAndValidators() {
        var services = new ServiceCollection();

        services.AddMailInboxApplication();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ISender>());
        Assert.Contains(
            provider.GetServices<IValidator<GetInboundMailMessagesQuery>>(),
            validator => validator is GetInboundMailMessagesQueryValidator);
        Assert.Contains(
            provider.GetServices<IValidator<MarkInboundMailMessageReadCommand>>(),
            validator => validator is MarkInboundMailMessageReadCommandValidator);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingInboundMailStore : IInboundMailStore {
        public IReadOnlyList<InboundMailMessageSummary> MessageSummaries { get; init; } = [];
        public InboundMailMessageDetails? Details { get; init; }
        public int LastMessagesLimit { get; private set; }
        public Guid LastMessageId { get; private set; }
        public DateTimeOffset LastReadAtUtc { get; private set; }
        public CancellationToken LastMessagesCancellationToken { get; private set; }

        public Task<InboundMailSaveResult> SaveAsync(InboundMailMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(int limit, CancellationToken cancellationToken) {
            LastMessagesLimit = limit;
            LastMessagesCancellationToken = cancellationToken;
            return Task.FromResult(MessageSummaries);
        }

        public Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Details is not null && Details.Id == id ? Details : null);

        public Task<bool> MarkAsReadAsync(Guid id, DateTimeOffset readAtUtc, CancellationToken cancellationToken) {
            LastMessageId = id;
            LastReadAtUtc = readAtUtc;
            LastMessagesCancellationToken = cancellationToken;
            return Task.FromResult(Details is not null && Details.Id == id);
        }

        public Task<InboundMailRetentionResult> PurgeExpiredAsync(
            DateTimeOffset contentCutoffUtc,
            DateTimeOffset metadataCutoffUtc,
            int batchSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingReadinessChecker : IMailInboxReadinessChecker {
        public bool Called { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task CheckReadyAsync(CancellationToken cancellationToken) {
            Called = true;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private static IReadOnlyList<string> CaptureOutcomes(Action record) {
        var outcomes = new List<string>();
        using var listener = new MeterListener {
            InstrumentPublished = (instrument, meterListener) => {
                if (string.Equals(instrument.Meter.Name, MailInboxTelemetry.MeterName, StringComparison.Ordinal)) {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => outcomes.Add(GetOutcome(tags)));
        listener.SetMeasurementEventCallback<double>((_, _, tags, _) => outcomes.Add(GetOutcome(tags)));
        listener.Start();

        record();
        return outcomes;
    }

    private static string GetOutcome(ReadOnlySpan<KeyValuePair<string, object?>> tags) {
        KeyValuePair<string, object?> tag = Assert.Single(tags.ToArray());
        Assert.Equal("fooddiary.mailinbox.outcome", tag.Key);
        return Assert.IsType<string>(tag.Value);
    }

    private static InboundMailMessageDetails CreateDetails(Guid id) =>
        new(
            id,
            "message-id",
            "sender@example.com",
            ["admin@fooddiary.club"],
            "Hello",
            "text",
            "<p>text</p>",
            "raw",
            InboundMailMessageCategories.General,
            DmarcReport: null,
            InboundMailMessageStatus.Received.ToString(),
            ReadAtUtc: null,
            DateTimeOffset.UtcNow,
            ContentPurgedAtUtc: null);

    [ExcludeFromCodeCoverage]
    private sealed record TestCommand : IRequest<Result>;

    [ExcludeFromCodeCoverage]
    private sealed record TestUnsupportedResultCommand : IRequest<UnsupportedResult>;

    [ExcludeFromCodeCoverage]
    private sealed class AlwaysFailingTestCommandValidator : AbstractValidator<TestCommand> {
        public AlwaysFailingTestCommandValidator() {
            RuleFor(static command => command)
                .Custom(static (_, context) => context.AddFailure(new ValidationFailure("Name", "Required") {
                    ErrorCode = "",
                }));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class AlwaysFailingUnsupportedResultCommandValidator : AbstractValidator<TestUnsupportedResultCommand> {
        public AlwaysFailingUnsupportedResultCommandValidator() {
            RuleFor(static command => command)
                .Custom(static (_, context) => context.AddFailure("Name", "Required"));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class MultipleFailuresTestCommandValidator : AbstractValidator<TestCommand> {
        public MultipleFailuresTestCommandValidator() {
            RuleFor(static command => command)
                .Custom(static (_, context) => {
                    context.AddFailure("Name", "First");
                    context.AddFailure("Name", "Second");
                    context.AddFailure("Name", "Second");
                });
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class UnsupportedResult() : Result(isSuccess: true, Error.None);

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
