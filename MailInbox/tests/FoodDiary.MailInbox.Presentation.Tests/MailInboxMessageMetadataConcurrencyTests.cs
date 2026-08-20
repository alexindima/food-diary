using FoodDiary.MailInbox.Application.Messages.Commands.MarkInboundMailMessageRead;
using FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessages;
using FoodDiary.MailInbox.Presentation.Features.Messages;
using FoodDiary.MailInbox.Presentation.Filters;
using FoodDiary.MailInbox.Presentation.Options;
using FoodDiary.MailInbox.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace FoodDiary.MailInbox.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxMessageMetadataConcurrencyTests {
    private const string ValidApiKey = "0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task Gate_WhenCapacityIsBusy_TimesOutAndRecoversAfterRelease() {
        using MailInboxMessageMetadataConcurrencyGate gate = CreateGate(TimeSpan.FromMilliseconds(1));
        IDisposable? first = await gate.TryEnterAsync(CancellationToken.None);
        Assert.NotNull(first);

        IDisposable? limited = await gate.TryEnterAsync(CancellationToken.None);

        Assert.Null(limited);
        first.Dispose();
        IDisposable? recovered = await gate.TryEnterAsync(CancellationToken.None);
        Assert.NotNull(recovered);
        recovered.Dispose();
        recovered.Dispose();
    }

    [Fact]
    public async Task Filter_WhenCapacityIsAvailable_InvokesNextAndReleasesLease() {
        using MailInboxMessageMetadataConcurrencyGate gate = CreateGate(TimeSpan.FromMilliseconds(25));
        var filter = new MailInboxMessageMetadataConcurrencyFilter(gate);
        ResourceExecutingContext context = CreateExecutingContext();
        bool invoked = false;

        await filter.OnResourceExecutionAsync(context, () => {
            invoked = true;
            return Task.FromResult(new ResourceExecutedContext(context, []));
        });

        Assert.True(invoked);
        IDisposable? recovered = await gate.TryEnterAsync(CancellationToken.None);
        Assert.NotNull(recovered);
        recovered.Dispose();
    }

    [Fact]
    public async Task Filter_WhenCapacityIsBusy_ReturnsTooManyRequestsWithoutInvokingNext() {
        using MailInboxMessageMetadataConcurrencyGate gate = CreateGate(TimeSpan.FromMilliseconds(1));
        using IDisposable? held = await gate.TryEnterAsync(CancellationToken.None);
        Assert.NotNull(held);
        var filter = new MailInboxMessageMetadataConcurrencyFilter(gate);
        ResourceExecutingContext context = CreateExecutingContext();
        bool invoked = false;

        await filter.OnResourceExecutionAsync(context, () => {
            invoked = true;
            return Task.FromResult(new ResourceExecutedContext(context, []));
        });

        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        MailInboxApiErrorHttpResponse response = Assert.IsType<MailInboxApiErrorHttpResponse>(result.Value);
        Assert.Multiple(
            () => Assert.False(invoked),
            () => Assert.Equal(StatusCodes.Status429TooManyRequests, result.StatusCode),
            () => Assert.Equal("1", context.HttpContext.Response.Headers.RetryAfter),
            () => Assert.Equal("MailInbox.MessageMetadataCapacityExceeded", response.Error));
    }

    [Theory]
    [InlineData(nameof(MailInboxMessagesController.Get))]
    [InlineData(nameof(MailInboxMessagesController.MarkRead))]
    public void MetadataActions_UseConcurrencyFilter(string actionName) {
        ServiceFilterAttribute attribute = Assert.Single(
            typeof(MailInboxMessagesController)
                .GetMethod(actionName)!
                .GetCustomAttributes(typeof(ServiceFilterAttribute), inherit: true)
                .Cast<ServiceFilterAttribute>());

        Assert.Equal(typeof(MailInboxMessageMetadataConcurrencyFilter), attribute.ServiceType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MetadataAction_WhenExecutionTimesOut_ReturnsSafeServiceUnavailable(bool listOperation) {
        MailInboxMessagesController controller = CreateController(
            new BlockingSender(),
            TimeSpan.FromMilliseconds(1));

        IActionResult actionResult = listOperation
            ? await controller.Get(50)
            : await controller.MarkRead(Guid.NewGuid());

        ObjectResult result = Assert.IsType<ObjectResult>(actionResult);
        MailInboxApiErrorHttpResponse response = Assert.IsType<MailInboxApiErrorHttpResponse>(result.Value);
        Assert.Multiple(
            () => Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode),
            () => Assert.Equal("MailInbox.MessageMetadataTimedOut", response.Error),
            () => Assert.Equal("trace-metadata", response.TraceId));
    }

    [Fact]
    public async Task Get_WhenRequestIsCanceled_PropagatesCancellation() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        MailInboxMessagesController controller = CreateController(
            new BlockingSender(),
            TimeSpan.FromSeconds(1));
        controller.HttpContext.RequestAborted = cancellation.Token;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => controller.Get(50)).ConfigureAwait(true);
    }

    private static MailInboxMessageMetadataConcurrencyGate CreateGate(TimeSpan queueTimeout) =>
        new(Microsoft.Extensions.Options.Options.Create(new MailInboxHttpOptions {
            MetadataApiKey = ValidApiKey,
            MaxConcurrentMessageMetadataRequests = 1,
            MessageMetadataQueueTimeout = queueTimeout,
        }));

    private static MailInboxMessagesController CreateController(
        ISender sender,
        TimeSpan executionTimeout) =>
        new(
            sender,
            Microsoft.Extensions.Options.Options.Create(new MailInboxHttpOptions {
                MetadataApiKey = ValidApiKey,
                MessageMetadataExecutionTimeout = executionTimeout,
            })) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    TraceIdentifier = "trace-metadata",
                },
            },
        };

    private static ResourceExecutingContext CreateExecutingContext() {
        var httpContext = new DefaultHttpContext {
            TraceIdentifier = "trace-capacity",
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ResourceExecutingContext(actionContext, [], []);
    }

    private sealed class BlockingSender : ISender {
        public async Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default) {
            Assert.True(
                request is GetInboundMailMessagesQuery or MarkInboundMailMessageReadCommand,
                $"Unexpected request type: {request.GetType().Name}");
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("The blocking sender should always be canceled.");
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
