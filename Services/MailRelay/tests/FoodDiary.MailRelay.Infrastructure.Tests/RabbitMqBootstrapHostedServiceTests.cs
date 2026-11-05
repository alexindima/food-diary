using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.MailRelay.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class RabbitMqBootstrapHostedServiceTests {
    [Fact]
    public async Task SuccessfulDeclaration_CompletesAfterOneAttempt() {
        var options = new MailRelayBrokerOptions { Backend = MailRelayBrokerOptions.RabbitMqBackend };
        var broker = new RabbitMqMailRelayBroker(MsOptions.Create(options), NullLogger<RabbitMqMailRelayBroker>.Instance);
        int declarations = 0;
        using var service = new RabbitMqMailRelayBootstrapHostedService(
            broker,
            MsOptions.Create(options),
            NullLogger<RabbitMqMailRelayBootstrapHostedService>.Instance,
            token => {
                Assert.False(token.IsCancellationRequested);
                declarations++;
                return Task.CompletedTask;
            });

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, declarations);
        Assert.True(service.ExecuteTask.IsCompletedSuccessfully);
    }
}
