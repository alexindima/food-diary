using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Client;
using FoodDiary.MailRelay.Client.Extensions;
using FoodDiary.MailRelay.Client.Options;
using FoodDiary.MailRelay.Infrastructure.Extensions;
using FoodDiary.MailRelay.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayInfrastructureOptionsTests {
    [Fact]
    public void MailRelayQueueOptions_HasValidConfiguration_ReturnsFalseForInvalidRetryShape() {
        var options = new MailRelayQueueOptions {
            PollIntervalSeconds = 1,
            BatchSize = 1,
            MaxAttempts = 1,
            BaseRetryDelaySeconds = 30,
            MaxRetryDelaySeconds = 10,
            LockTimeoutSeconds = 1
        };

        Assert.False(MailRelayQueueOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void MailRelayBrokerOptions_ValidatesSupportedBackendsAndRequiredRabbitMqFields() {
        Assert.False(MailRelayBrokerOptions.HasSupportedBackend(new MailRelayBrokerOptions {
            Backend = "Unknown"
        }));
        Assert.True(MailRelayBrokerOptions.HasValidConfiguration(new MailRelayBrokerOptions {
            Backend = MailRelayBrokerOptions.PostgresPollingBackend
        }));
        Assert.False(MailRelayBrokerOptions.HasValidConfiguration(new MailRelayBrokerOptions {
            QueueName = ""
        }));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("https://otel.example.test", true)]
    [InlineData("not a uri", false)]
    public void OpenTelemetryOptions_HasValidOtlpEndpoint_ReturnsExpectedResult(string? endpoint, bool expected) {
        var options = new OpenTelemetryOptions {
            Otlp = new OpenTelemetryOptions.OtlpOptions {
                Endpoint = endpoint
            }
        };

        Assert.Equal(expected, OpenTelemetryOptions.HasValidOtlpEndpoint(options));
    }

    [Fact]
    public void AddMailRelayOptions_RegistersAllOptionsWithoutResolvingInfrastructure() {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["MailRelay:RequireApiKey"] = "true",
                ["MailRelay:ApiKey"] = "secret",
                ["RelaySmtp:Port"] = "587",
                ["MailRelayDelivery:Mode"] = MailRelayDeliveryOptions.DirectMxMode,
                ["DirectMx:Port"] = "25",
                ["DirectMx:ConnectTimeoutSeconds"] = "10",
                ["MailRelayQueue:PollIntervalSeconds"] = "1",
                ["MailRelayQueue:BatchSize"] = "2",
                ["MailRelayQueue:MaxAttempts"] = "3",
                ["MailRelayQueue:BaseRetryDelaySeconds"] = "5",
                ["MailRelayQueue:MaxRetryDelaySeconds"] = "10",
                ["MailRelayQueue:LockTimeoutSeconds"] = "20",
                ["MailRelayBroker:Backend"] = MailRelayBrokerOptions.PostgresPollingBackend
            })
            .Build();
        var services = new ServiceCollection();

        services.AddMailRelayOptions(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.True(provider.GetRequiredService<IOptions<MailRelayQueueOptions>>().Value.BatchSize > 0);
        Assert.Equal(MailRelayBrokerOptions.PostgresPollingBackend,
            provider.GetRequiredService<IOptions<MailRelayBrokerOptions>>().Value.Backend);
    }

    [Fact]
    public void AddMailRelayServices_RegistersInfrastructureAbstractionsWithoutCreatingExternalConnections() {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=test;Password=test"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddOptions();
        services.AddMailRelayServices(configuration);

        Assert.Contains(services, static descriptor => descriptor.ServiceType == typeof(IMailRelayQueueStore));
        Assert.Contains(services, static descriptor => descriptor.ServiceType == typeof(IRelayDeliveryTransport));
        Assert.Contains(services, static descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void AddMailRelayClient_RegistersTypedClientAndOptions() {
        var services = new ServiceCollection();

        services.AddMailRelayClient(options => {
            options.BaseUrl = "https://relay.example.test";
            options.Timeout = TimeSpan.FromSeconds(5);
        });
        using var provider = services.BuildServiceProvider();

        Assert.Equal("https://relay.example.test", provider.GetRequiredService<IOptions<MailRelayClientOptions>>().Value.BaseUrl);
        Assert.NotNull(provider.GetRequiredService<IMailRelayClient>());
    }
}
