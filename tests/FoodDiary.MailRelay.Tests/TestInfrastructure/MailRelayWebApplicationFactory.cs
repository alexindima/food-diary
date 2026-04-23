using FoodDiary.MailRelay.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.MailRelay.Tests.TestInfrastructure;

public sealed class MailRelayWebApplicationFactory(
    MailRelayEnvironmentFixture fixture,
    RecordingRelayDeliveryTransport recordingTransport) : WebApplicationFactory<Program> {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        var databaseConnectionString = fixture.CreateIsolatedDatabaseAsync().GetAwaiter().GetResult();

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) => {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:DefaultConnection"] = databaseConnectionString,
                ["MailRelay:RequireApiKey"] = "false",
                ["MailRelayBroker:Backend"] = "RabbitMq",
                ["MailRelayBroker:HostName"] = fixture.RabbitMqHostName,
                ["MailRelayBroker:Port"] = fixture.RabbitMqPort.ToString(),
                ["MailRelayBroker:UserName"] = "guest",
                ["MailRelayBroker:Password"] = "guest",
                ["MailRelayBroker:VirtualHost"] = "/",
                ["MailRelayBroker:QueueName"] = $"fooddiary.mailrelay.outbound.{Guid.NewGuid():N}",
                ["MailRelayBroker:RetryQueueName"] = $"fooddiary.mailrelay.retry.{Guid.NewGuid():N}",
                ["MailRelayBroker:DeadLetterQueueName"] = $"fooddiary.mailrelay.dead.{Guid.NewGuid():N}",
                ["MailRelayBroker:RetryDelayMilliseconds"] = "250",
                ["MailRelayQueue:PollIntervalSeconds"] = "1",
                ["MailRelayQueue:BatchSize"] = "10",
                ["MailRelayQueue:MaxAttempts"] = "3",
                ["MailRelayQueue:BaseRetryDelaySeconds"] = "1",
                ["MailRelayQueue:MaxRetryDelaySeconds"] = "2",
                ["MailRelayQueue:LockTimeoutSeconds"] = "30"
            });
        });

        builder.ConfigureServices(services => {
            services.RemoveAll<IRelayDeliveryTransport>();
            services.AddSingleton<IRelayDeliveryTransport>(recordingTransport);
        });
    }
}
