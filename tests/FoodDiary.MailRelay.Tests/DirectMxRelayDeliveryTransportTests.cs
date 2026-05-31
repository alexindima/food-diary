using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Tests;

public sealed class DirectMxRelayDeliveryTransportTests {
    [Fact]
    public async Task SendAsync_WhenMxHostIsLoopbackAddress_RejectsBeforeDelivery() {
        var transport = new DirectMxRelayDeliveryTransport(
            Options.Create(new DirectMxOptions {
                Port = 25,
                ConnectTimeoutSeconds = 1,
                UseStartTlsWhenAvailable = false,
            }),
            new StubMxResolver([new MxRecord("127.0.0.1", 0)]),
            new DkimSigningService(Options.Create(new MailRelayDkimOptions())),
            NullLogger<DirectMxRelayDeliveryTransport>.Instance);
        var request = new RelayEmailMessageRequest(
            "sender@example.com",
            "Sender",
            ["recipient@example.com"],
            "Subject",
            "<p>Hello</p>",
            "Hello");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            transport.SendAsync(request, CancellationToken.None));

        Assert.Contains("private or loopback", ex.InnerException?.Message ?? ex.Message);
    }

    private sealed class StubMxResolver(IReadOnlyList<MxRecord> records) : IMxResolver {
        public Task<IReadOnlyList<MxRecord>> ResolveAsync(string domain, CancellationToken cancellationToken) =>
            Task.FromResult(records);
    }
}
