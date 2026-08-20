using FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessageDetails;
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
public sealed class MailInboxMessageDetailConcurrencyTests {
    [Fact]
    public async Task Gate_WhenCapacityIsBusy_TimesOutAndRecoversAfterRelease() {
        using var gate = new MailInboxMessageDetailConcurrencyGate(
            Microsoft.Extensions.Options.Options.Create(new MailInboxHttpOptions {
                MetadataApiKey = "test-key",
                MaxConcurrentMessageDetailRequests = 1,
                MessageDetailQueueTimeout = TimeSpan.FromMilliseconds(25),
            }));

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
    public void GetById_UsesMessageDetailConcurrencyFilter() {
        ServiceFilterAttribute attribute = Assert.Single(
            typeof(MailInboxMessagesController)
                .GetMethod(nameof(MailInboxMessagesController.GetById))!
                .GetCustomAttributes(typeof(ServiceFilterAttribute), inherit: true)
                .Cast<ServiceFilterAttribute>());

        Assert.Equal(typeof(MailInboxMessageDetailConcurrencyFilter), attribute.ServiceType);
    }

    [Fact]
    public async Task GetById_WhenExecutionTimesOut_ReturnsSafeServiceUnavailable() {
        MailInboxMessagesController controller = CreateController(
            new BlockingSender(),
            TimeSpan.FromMilliseconds(1));

        IActionResult actionResult = await controller.GetById(Guid.NewGuid());

        ObjectResult result = Assert.IsType<ObjectResult>(actionResult);
        MailInboxApiErrorHttpResponse response = Assert.IsType<MailInboxApiErrorHttpResponse>(result.Value);
        Assert.Multiple(
            () => Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode),
            () => Assert.Equal("MailInbox.MessageDetailTimedOut", response.Error),
            () => Assert.Equal("trace-detail", response.TraceId));
    }

    [Fact]
    public async Task GetById_WhenRequestIsCanceled_PropagatesCancellation() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        MailInboxMessagesController controller = CreateController(
            new BlockingSender(),
            TimeSpan.FromSeconds(1));
        controller.HttpContext.RequestAborted = cancellation.Token;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => controller.GetById(Guid.NewGuid())).ConfigureAwait(true);
    }

    [Fact]
    public async Task Filter_WhenCapacityIsAvailable_InvokesNextAndReleasesLease() {
        using MailInboxMessageDetailConcurrencyGate gate = CreateGate(TimeSpan.FromMilliseconds(25));
        var filter = new MailInboxMessageDetailConcurrencyFilter(gate);
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
        using MailInboxMessageDetailConcurrencyGate gate = CreateGate(TimeSpan.FromMilliseconds(1));
        using IDisposable? held = await gate.TryEnterAsync(CancellationToken.None);
        Assert.NotNull(held);
        var filter = new MailInboxMessageDetailConcurrencyFilter(gate);
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
            () => Assert.Equal("MailInbox.MessageDetailCapacityExceeded", response.Error));
    }

    private static MailInboxMessageDetailConcurrencyGate CreateGate(TimeSpan queueTimeout) =>
        new(Microsoft.Extensions.Options.Options.Create(new MailInboxHttpOptions {
            MetadataApiKey = "0123456789abcdef0123456789abcdef",
            MaxConcurrentMessageDetailRequests = 1,
            MessageDetailQueueTimeout = queueTimeout,
        }));

    private static MailInboxMessagesController CreateController(
        ISender sender,
        TimeSpan executionTimeout) =>
        new(
            sender,
            Microsoft.Extensions.Options.Options.Create(new MailInboxHttpOptions {
                MetadataApiKey = "0123456789abcdef0123456789abcdef",
                MessageDetailExecutionTimeout = executionTimeout,
            })) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    TraceIdentifier = "trace-detail",
                },
            },
        };

    private static ResourceExecutingContext CreateExecutingContext() {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());
        return new ResourceExecutingContext(actionContext, [], []);
    }

    private sealed class BlockingSender : ISender {
        public async Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default) {
            Assert.IsType<GetInboundMailMessageDetailsQuery>(request);
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
