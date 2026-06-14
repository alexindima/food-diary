using System.Text;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using FoodDiary.MailRelay.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using RabbitMQ.Client;

namespace FoodDiary.MailRelay.Tests;

[Collection("mailrelay-environment")]
[ExcludeFromCodeCoverage]
public sealed class RabbitMqMailRelayBrokerIntegrationTests(MailRelayEnvironmentFixture fixture) {
    [RequiresDockerFact]
    public async Task Broker_DeclaresTopologyAndRoutesOutboundDeadLetterAndRetryMessages() {
        fixture.EnsureAvailable();
        MailRelayBrokerOptions options = CreateOptions();
        var broker = new RabbitMqMailRelayBroker(
            Options.Create(options),
            NullLogger<RabbitMqMailRelayBroker>.Instance);

        await broker.DeclareTopologyAsync(CancellationToken.None);
        await broker.CheckReadyAsync(CancellationToken.None);

        var outboundId = Guid.NewGuid();
        await broker.PublishOutboundAsync(outboundId, CancellationToken.None);
        Assert.Equal(outboundId.ToString("D"), await WaitForRabbitMessageAsync(options, options.QueueName));

        var deadLetterId = Guid.NewGuid();
        await broker.PublishDeadLetterAsync(deadLetterId, CancellationToken.None);
        Assert.Equal(deadLetterId.ToString("D"), await WaitForRabbitMessageAsync(options, options.DeadLetterQueueName));

        var retryId = Guid.NewGuid();
        await broker.PublishRetryAsync(retryId, TimeSpan.FromMilliseconds(50), CancellationToken.None);
        Assert.Equal(retryId.ToString("D"), await WaitForRabbitMessageAsync(options, options.QueueName));
    }

    [RequiresDockerFact]
    public async Task ReadinessChecker_WhenRabbitMqBackendIsReady_CompletesPostgresAndRabbitChecks() {
        fixture.EnsureAvailable();
        MailRelayBrokerOptions options = CreateOptions();
        var broker = new RabbitMqMailRelayBroker(
            Options.Create(options),
            NullLogger<RabbitMqMailRelayBroker>.Instance);
        await broker.DeclareTopologyAsync(CancellationToken.None).ConfigureAwait(false);
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateIsolatedDatabaseAsync().ConfigureAwait(false));
        await using (dataSource.ConfigureAwait(false)) {
            var checker = new MailRelayReadinessChecker(
                dataSource,
                broker,
                Options.Create(options));

            await checker.CheckReadyAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private MailRelayBrokerOptions CreateOptions() {
        string suffix = Guid.NewGuid().ToString("N");
        return new MailRelayBrokerOptions {
            Backend = MailRelayBrokerOptions.RabbitMqBackend,
            HostName = fixture.RabbitMqHostName,
            Port = fixture.RabbitMqPort,
            UserName = "guest",
            Password = "guest",
            QueueName = $"fooddiary.mailrelay.test.outbound.{suffix}",
            OutboundExchangeName = $"fooddiary.mailrelay.test.{suffix}",
            RetryExchangeName = $"fooddiary.mailrelay.test.retry.{suffix}",
            RetryQueueName = $"fooddiary.mailrelay.test.outbound.retry.{suffix}",
            RetryDelayMilliseconds = 50,
            DeadLetterExchangeName = $"fooddiary.mailrelay.test.dead.{suffix}",
            DeadLetterQueueName = $"fooddiary.mailrelay.test.outbound.dead.{suffix}",
        };
    }

    private static async Task<string> WaitForRabbitMessageAsync(
        MailRelayBrokerOptions options,
        string queueName) {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline) {
            string? message = await TryGetRabbitMessageAsync(options, queueName).ConfigureAwait(false);
            if (message is not null) {
                return message;
            }

            await Task.Delay(100).ConfigureAwait(false);
        }

        throw new TimeoutException($"RabbitMQ message was not available in queue '{queueName}'.");
    }

    private static async Task<string?> TryGetRabbitMessageAsync(
        MailRelayBrokerOptions options,
        string queueName) {
        var factory = new ConnectionFactory {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
        };
        IConnection connection = await factory.CreateConnectionAsync(CancellationToken.None).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            IChannel channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken.None).ConfigureAwait(false);
            await using (channel.ConfigureAwait(false)) {
                BasicGetResult? result = await channel.BasicGetAsync(
                    queueName,
                    autoAck: true,
                    cancellationToken: CancellationToken.None).ConfigureAwait(false);

                return result is null ? null : Encoding.UTF8.GetString(result.Body.ToArray());
            }
        }
    }
}
