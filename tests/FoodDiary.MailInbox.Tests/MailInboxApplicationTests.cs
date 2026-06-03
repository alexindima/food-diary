using FluentValidation;
using FluentValidation.Results;
using FoodDiary.MailInbox.Application;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Common.Behaviors;
using FoodDiary.MailInbox.Application.Common.Result;
using FoodDiary.MailInbox.Application.Health;
using FoodDiary.MailInbox.Application.Messages.Commands;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Application.Messages.Queries;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.MailInbox.Tests;

public sealed class MailInboxApplicationTests {
    [Fact]
    public async Task ReceiveInboundMailHandler_SavesReceivedAggregateAndReturnsStoreId() {
        var store = new RecordingInboundMailStore();
        var handler = new ReceiveInboundMailCommandHandler(store);
        var receivedAt = new DateTimeOffset(2026, 5, 6, 10, 0, 0, TimeSpan.Zero);

        var result = await handler.Handle(
            new ReceiveInboundMailCommand(new ReceiveInboundMailRequest(
                MessageId: " message-id ",
                FromAddress: " sender@example.com ",
                ToRecipients: [" admin@fooddiary.club "],
                Subject: " Hello ",
                TextBody: "Text",
                HtmlBody: "<p>Text</p>",
                RawMime: "raw mime",
                ReceivedAtUtc: receivedAt)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(store.SavedId, result.Value);
        Assert.NotNull(store.LastSaved);
        Assert.Equal("message-id", store.LastSaved.MessageId);
        Assert.Equal("sender@example.com", store.LastSaved.FromAddress);
        Assert.Equal(["admin@fooddiary.club"], store.LastSaved.ToRecipients);
        Assert.Equal("Hello", store.LastSaved.Subject);
        Assert.Equal("raw mime", store.LastSaved.RawMime);
        Assert.Equal(receivedAt, store.LastSaved.ReceivedAtUtc);
    }

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
            DateTimeOffset.UtcNow);
        var store = new RecordingInboundMailStore {
            MessageSummaries = [expected],
        };
        var handler = new GetInboundMailMessagesQueryHandler(store);

        var result = await handler.Handle(new GetInboundMailMessagesQuery(25), cts.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, Assert.Single(result.Value));
        Assert.Equal(25, store.LastMessagesLimit);
        Assert.Equal(cts.Token, store.LastMessagesCancellationToken);
    }

    [Fact]
    public async Task GetInboundMailMessageDetailsHandler_WhenMissing_ReturnsNotFound() {
        var id = Guid.NewGuid();
        var handler = new GetInboundMailMessageDetailsQueryHandler(new RecordingInboundMailStore());

        var result = await handler.Handle(new GetInboundMailMessageDetailsQuery(id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("MailInbox.Message.NotFound", result.Error?.Code);
        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
    }

    [Fact]
    public async Task CheckMailInboxReadinessHandler_ForwardsCancellationToken() {
        using var cts = new CancellationTokenSource();
        var checker = new RecordingReadinessChecker();
        var handler = new CheckMailInboxReadinessQueryHandler(checker);

        var result = await handler.Handle(new CheckMailInboxReadinessQuery(), cts.Token);

        Assert.True(result.IsSuccess);
        Assert.True(checker.Called);
        Assert.Equal(cts.Token, checker.CancellationToken);
    }

    [Fact]
    public async Task GetInboundMailMessageDetailsHandler_WhenFound_ReturnsDetails() {
        var id = Guid.NewGuid();
        var details = CreateDetails(id);
        var handler = new GetInboundMailMessageDetailsQueryHandler(new RecordingInboundMailStore {
            Details = details
        });

        var result = await handler.Handle(new GetInboundMailMessageDetailsQuery(id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(details, result.Value);
    }

    [Fact]
    public async Task ReceiveInboundMailValidator_WithMissingRecipientsAndRawMime_Fails() {
        var validator = new ReceiveInboundMailCommandValidator();

        var result = await validator.ValidateAsync(new ReceiveInboundMailCommand(
            new ReceiveInboundMailRequest(null, null, [], null, null, null, "", DateTimeOffset.UtcNow)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Request.ToRecipients", StringComparison.Ordinal));
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Request.RawMime", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(200, true)]
    [InlineData(0, false)]
    [InlineData(201, false)]
    public async Task GetInboundMailMessagesValidator_ValidatesLimitRange(int limit, bool expectedValid) {
        var validator = new GetInboundMailMessagesQueryValidator();

        var result = await validator.ValidateAsync(new GetInboundMailMessagesQuery(limit));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public async Task MailInboxValidationBehavior_WhenValidationFails_ReturnsTypedFailureAndDoesNotInvokeNext() {
        var behavior = new MailInboxValidationBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>(
            [new GetInboundMailMessagesQueryValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(
            new GetInboundMailMessagesQuery(0),
            _ => {
                nextCalled = true;
                return Task.FromResult(Result<IReadOnlyList<InboundMailMessageSummary>>.Success([]));
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(nextCalled);
        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
        Assert.Contains("Limit", result.Error?.Details?.Keys ?? []);
    }

    [Fact]
    public async Task MailInboxValidationBehavior_WhenNonGenericResultValidationFails_ReturnsFailure() {
        var behavior = new MailInboxValidationBehavior<TestCommand, Result>(
            [new AlwaysFailingTestCommandValidator()]);

        var result = await behavior.Handle(
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

        var result = await behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("One or more validation errors occurred.", result.Error?.Message);
        Assert.NotNull(result.Error?.Details);
        Assert.Equal(["First", "Second"], result.Error!.Details!["Name"]);
    }

    [Fact]
    public async Task MailInboxValidationBehavior_WhenNoValidators_InvokesNext() {
        var behavior = new MailInboxValidationBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>([]);
        var response = Result<IReadOnlyList<InboundMailMessageSummary>>.Success([]);

        var result = await behavior.Handle(
            new GetInboundMailMessagesQuery(10),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.Same(response, result);
    }

    [Fact]
    public async Task MailInboxValidationBehavior_WhenValidationSucceeds_InvokesNext() {
        var behavior = new MailInboxValidationBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>(
            [new GetInboundMailMessagesQueryValidator()]);
        var response = Result<IReadOnlyList<InboundMailMessageSummary>>.Success([]);

        var result = await behavior.Handle(
            new GetInboundMailMessagesQuery(10),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.Same(response, result);
    }

    [Fact]
    public async Task MailInboxLoggingBehavior_WhenNextSucceeds_ReturnsResponse() {
        var behavior = new MailInboxLoggingBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>(
            NullLogger<MailInboxLoggingBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>>.Instance);
        var response = Result<IReadOnlyList<InboundMailMessageSummary>>.Success([]);

        var result = await behavior.Handle(
            new GetInboundMailMessagesQuery(10),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.Same(response, result);
    }

    [Fact]
    public async Task MailInboxLoggingBehavior_WhenNextFails_ReturnsFailure() {
        var behavior = new MailInboxLoggingBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>(
            NullLogger<MailInboxLoggingBehavior<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>>>.Instance);
        var response = Result<IReadOnlyList<InboundMailMessageSummary>>.Failure(MailInboxErrors.MessageNotFound(Guid.NewGuid()));

        var result = await behavior.Handle(
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

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new GetInboundMailMessagesQuery(10),
            _ => throw exception,
            CancellationToken.None));

        Assert.Same(exception, thrown);
    }

    [Fact]
    public void AddMailInboxApplication_RegistersMediatorAndValidators() {
        var services = new ServiceCollection();

        services.AddMailInboxApplication();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ISender>());
        Assert.Contains(
            provider.GetServices<IValidator<GetInboundMailMessagesQuery>>(),
            validator => validator is GetInboundMailMessagesQueryValidator);
        Assert.Contains(
            provider.GetServices<IValidator<ReceiveInboundMailCommand>>(),
            validator => validator is ReceiveInboundMailCommandValidator);
    }

    [Fact]
    public void ResultFailure_CreatesFailedResult() {
        var error = MailInboxErrors.MessageNotFound(Guid.NewGuid());

        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void ResultConstructor_WhenStateIsInvalid_Throws() {
        Assert.Throws<InvalidOperationException>(() => new ExposedResult(isSuccess: true, MailInboxErrors.MessageNotFound(Guid.NewGuid())));
        Assert.Throws<InvalidOperationException>(() => new ExposedResult(isSuccess: false, error: null));
    }

    [Fact]
    public void ResultValue_WhenResultIsFailure_Throws() {
        var result = Result<string>.Failure(MailInboxErrors.MessageNotFound(Guid.NewGuid()));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    private sealed class RecordingInboundMailStore : IInboundMailStore {
        public Guid SavedId { get; } = Guid.NewGuid();
        public InboundMailMessage? LastSaved { get; private set; }
        public IReadOnlyList<InboundMailMessageSummary> MessageSummaries { get; init; } = [];
        public InboundMailMessageDetails? Details { get; init; }
        public int LastMessagesLimit { get; private set; }
        public CancellationToken LastMessagesCancellationToken { get; private set; }

        public Task<Guid> SaveAsync(InboundMailMessage message, CancellationToken cancellationToken) {
            LastSaved = message;
            return Task.FromResult(SavedId);
        }

        public Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(int limit, CancellationToken cancellationToken) {
            LastMessagesLimit = limit;
            LastMessagesCancellationToken = cancellationToken;
            return Task.FromResult(MessageSummaries);
        }

        public Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Details is not null && Details.Id == id ? Details : null);
    }

    private sealed class RecordingReadinessChecker : IMailInboxReadinessChecker {
        public bool Called { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task CheckReadyAsync(CancellationToken cancellationToken) {
            Called = true;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
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
            DateTimeOffset.UtcNow);

    private sealed record TestCommand : IRequest<Result>;

    private sealed class AlwaysFailingTestCommandValidator : AbstractValidator<TestCommand> {
        public AlwaysFailingTestCommandValidator() {
            RuleFor(static command => command)
                .Custom(static (_, context) => context.AddFailure(new ValidationFailure("Name", "Required") {
                    ErrorCode = ""
                }));
        }
    }

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

    private sealed class ExposedResult(bool isSuccess, MailInboxError? error) : Result(isSuccess, error);
}
