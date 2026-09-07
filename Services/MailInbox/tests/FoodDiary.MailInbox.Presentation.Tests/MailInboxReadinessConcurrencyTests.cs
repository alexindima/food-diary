using FoodDiary.Results;
using FoodDiary.MailInbox.Application.Health;
using FoodDiary.MailInbox.Presentation.Features.Health;
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
public sealed class MailInboxReadinessConcurrencyTests {
    private const string ValidApiKey = "0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task Gate_WhenCapacityIsBusy_TimesOutAndRecoversAfterRelease() {
        using MailInboxReadinessConcurrencyGate gate = CreateGate(TimeSpan.FromMilliseconds(1));
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
        using MailInboxReadinessConcurrencyGate gate = CreateGate(TimeSpan.FromMilliseconds(25));
        var filter = new MailInboxReadinessConcurrencyFilter(gate);
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
    public async Task Filter_WhenCapacityIsBusy_ReturnsServiceUnavailable() {
        using MailInboxReadinessConcurrencyGate gate = CreateGate(TimeSpan.FromMilliseconds(1));
        using IDisposable? held = await gate.TryEnterAsync(CancellationToken.None);
        Assert.NotNull(held);
        var filter = new MailInboxReadinessConcurrencyFilter(gate);
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
            () => Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode),
            () => Assert.Equal("1", context.HttpContext.Response.Headers.RetryAfter),
            () => Assert.Equal("MailInbox.ReadinessCapacityExceeded", response.Error));
    }

    [Fact]
    public void GetReady_UsesReadinessConcurrencyFilter() {
        ServiceFilterAttribute attribute = Assert.Single(
            typeof(MailInboxHealthController)
                .GetMethod(nameof(MailInboxHealthController.GetReady))!
                .GetCustomAttributes(typeof(ServiceFilterAttribute), inherit: true)
                .Cast<ServiceFilterAttribute>());

        Assert.Equal(typeof(MailInboxReadinessConcurrencyFilter), attribute.ServiceType);
    }

    [Fact]
    public async Task GetReady_WhenExecutionTimesOut_ReturnsSafeServiceUnavailable() {
        MailInboxHealthController controller = CreateController(
            new BlockingSender(),
            TimeSpan.FromMilliseconds(1));

        IActionResult actionResult = await controller.GetReady();

        ObjectResult result = Assert.IsType<ObjectResult>(actionResult);
        MailInboxApiErrorHttpResponse response = Assert.IsType<MailInboxApiErrorHttpResponse>(result.Value);
        Assert.Multiple(
            () => Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode),
            () => Assert.Equal("MailInbox.ReadinessTimedOut", response.Error),
            () => Assert.Equal("trace-readiness", response.TraceId));
    }

    [Fact]
    public async Task GetReady_WhenRequestIsCanceled_PropagatesCancellation() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        MailInboxHealthController controller = CreateController(
            new BlockingSender(),
            TimeSpan.FromSeconds(1));
        controller.HttpContext.RequestAborted = cancellation.Token;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => controller.GetReady()).ConfigureAwait(true);
    }

    private static MailInboxReadinessConcurrencyGate CreateGate(TimeSpan queueTimeout) =>
        new(Microsoft.Extensions.Options.Options.Create(new MailInboxHttpOptions {
            MetadataApiKey = ValidApiKey,
            MaxConcurrentReadinessRequests = 1,
            ReadinessQueueTimeout = queueTimeout,
        }));

    private static MailInboxHealthController CreateController(ISender sender, TimeSpan executionTimeout) =>
        new(
            sender,
            Microsoft.Extensions.Options.Options.Create(new MailInboxHttpOptions {
                MetadataApiKey = ValidApiKey,
                ReadinessExecutionTimeout = executionTimeout,
            })) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    TraceIdentifier = "trace-readiness",
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
            Assert.IsType<CheckMailInboxReadinessQuery>(request);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return (TResponse)(object)Result.Success();
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
