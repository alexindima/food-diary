using FoodDiary.MailInbox.Presentation.Features.Messages;
using FoodDiary.MailInbox.Presentation.Filters;
using FoodDiary.MailInbox.Presentation.Options;
using Microsoft.AspNetCore.Mvc;

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
}
