using FoodDiary.MailInbox.Presentation.Features.Messages;
using FoodDiary.MailInbox.Presentation.Filters;
using FoodDiary.MailInbox.Presentation.Options;
using FoodDiary.MailInbox.Presentation.Responses;
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
                ApiKey = "test-key",
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
            ApiKey = "0123456789abcdef0123456789abcdef",
            MaxConcurrentMessageDetailRequests = 1,
            MessageDetailQueueTimeout = queueTimeout,
        }));

    private static ResourceExecutingContext CreateExecutingContext() {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());
        return new ResourceExecutingContext(actionContext, [], []);
    }
}
