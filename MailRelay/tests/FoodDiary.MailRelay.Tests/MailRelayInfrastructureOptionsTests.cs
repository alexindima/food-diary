using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Application.Common.Results;
using FoodDiary.MailRelay.Application.Options;
using FoodDiary.MailRelay.Client;
using FoodDiary.MailRelay.Client.Extensions;
using FoodDiary.MailRelay.Client.Options;
using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.MailRelay.Infrastructure.Extensions;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
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
            LockTimeoutSeconds = 1,
        };

        Assert.False(MailRelayQueueOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void MailRelayBrokerOptions_ValidatesSupportedBackendsAndRequiredRabbitMqFields() {
        Assert.False(MailRelayBrokerOptions.HasSupportedBackend(new MailRelayBrokerOptions {
            Backend = "Unknown",
        }));
        Assert.True(MailRelayBrokerOptions.HasValidConfiguration(new MailRelayBrokerOptions {
            Backend = MailRelayBrokerOptions.PostgresPollingBackend,
        }));
        Assert.False(MailRelayBrokerOptions.HasValidConfiguration(new MailRelayBrokerOptions {
            QueueName = "",
        }));
    }

    [Fact]
    public void MailRelayOptions_WhenMailgunSignatureIsRequired_RequiresSigningKey() {
        Assert.False(MailRelayOptions.HasValidProviderWebhookConfiguration(new MailRelayOptions {
            RequireMailgunWebhookSignature = true,
            MailgunWebhookSigningKey = "",
        }));
        Assert.True(MailRelayOptions.HasValidProviderWebhookConfiguration(new MailRelayOptions {
            RequireMailgunWebhookSignature = true,
            MailgunWebhookSigningKey = "secret",
        }));
        Assert.True(MailRelayOptions.HasValidProviderWebhookConfiguration(new MailRelayOptions {
            RequireMailgunWebhookSignature = false,
            MailgunWebhookSigningKey = "",
        }));
    }

    [Fact]
    public void ConfiguredMailRelayDeliveryPolicy_WhenDirectMxRecipientsSpanDomains_ReturnsValidationFailure() {
        var policy = new ConfiguredMailRelayDeliveryPolicy(Options.Create(new MailRelayDeliveryOptions {
            Mode = MailRelayDeliveryOptions.DirectMxMode,
        }));

        Result result = policy.CanEnqueue(new RelayEmailMessageRequest(
            "relay@example.com",
            "FoodDiary",
            ["first@example.com", "second@example.net"],
            "Subject",
            "<p>Hello</p>",
            "Hello"));

        Assert.True(result.IsFailure);
        Assert.Equal("MailRelay.Delivery.DirectMxMultipleRecipientDomains", result.Error?.Code);
    }

    [Fact]
    public void ConfiguredMailRelayDeliveryPolicy_WhenModeIsNotDirectMx_AllowsMultipleDomains() {
        var policy = new ConfiguredMailRelayDeliveryPolicy(Options.Create(new MailRelayDeliveryOptions {
            Mode = MailRelayDeliveryOptions.SmtpSubmissionMode,
        }));

        Result result = policy.CanEnqueue(new RelayEmailMessageRequest(
            "relay@example.com",
            "FoodDiary",
            ["first@example.com", "second@example.net"],
            "Subject",
            "<p>Hello</p>",
            "Hello"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task NoOpMailRelayDispatchNotifier_CompletesWithoutPublishing() {
        var notifier = new NoOpMailRelayDispatchNotifier();

        await notifier.NotifyQueuedAsync(Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task RabbitMqMailRelayDispatchNotifier_WhenBrokerIsDisabled_CompletesWithoutPublishing() {
        var broker = new RabbitMqMailRelayBroker(
            Options.Create(new MailRelayBrokerOptions {
                Backend = MailRelayBrokerOptions.PostgresPollingBackend,
            }),
            NullLogger<RabbitMqMailRelayBroker>.Instance);
        var notifier = new RabbitMqMailRelayDispatchNotifier(
            broker,
            NullLogger<RabbitMqMailRelayDispatchNotifier>.Instance);

        await notifier.NotifyQueuedAsync(Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task RabbitMqMailRelayBootstrapHostedService_WhenBrokerIsDisabled_StartsAndStopsWithoutDeclaringTopology() {
        var broker = new RabbitMqMailRelayBroker(
            Options.Create(new MailRelayBrokerOptions {
                Backend = MailRelayBrokerOptions.PostgresPollingBackend,
            }),
            NullLogger<RabbitMqMailRelayBroker>.Instance);
        var service = new RabbitMqMailRelayBootstrapHostedService(
            broker,
            NullLogger<RabbitMqMailRelayBootstrapHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ConfigurableRelayDeliveryTransport_WhenModeIsUnsupported_Throws() {
        var transport = new ConfigurableRelayDeliveryTransport(
            smtpTransport: null!,
            directMxTransport: null!,
            Options.Create(new MailRelayDeliveryOptions {
                Mode = "Unknown",
            }));

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            transport.SendAsync(new RelayEmailMessageRequest(
                "relay@example.com",
                "FoodDiary",
                ["user@example.com"],
                "Subject",
                "<p>Hello</p>",
                "Hello"), CancellationToken.None));

        Assert.Contains("Unsupported", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("https://otel.example.test", true)]
    [InlineData("not a uri", false)]
    public void OpenTelemetryOptions_HasValidOtlpEndpoint_ReturnsExpectedResult(string? endpoint, bool expected) {
        var options = new OpenTelemetryOptions {
            Otlp = new OpenTelemetryOptions.OtlpOptions {
                Endpoint = endpoint,
            },
        };

        Assert.Equal(expected, OpenTelemetryOptions.HasValidOtlpEndpoint(options));
    }

    [Fact]
    public void AddMailRelayOptions_RegistersAllOptionsWithoutResolvingInfrastructure() {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["MailRelay:RequireApiKey"] = "true",
                ["MailRelay:ApiKey"] = "secret",
                ["MailRelay:MailgunWebhookSigningKey"] = "mailgun-secret",
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
                ["MailRelayBroker:Backend"] = MailRelayBrokerOptions.PostgresPollingBackend,
            })
            .Build();
        var services = new ServiceCollection();

        services.AddMailRelayOptions(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.True(provider.GetRequiredService<IOptions<MailRelayQueueOptions>>().Value.BatchSize > 0);
        Assert.Equal(MailRelayBrokerOptions.PostgresPollingBackend,
            provider.GetRequiredService<IOptions<MailRelayBrokerOptions>>().Value.Backend);
    }

    [Fact]
    public void AddMailRelayServices_RegistersInfrastructureAbstractionsWithoutCreatingExternalConnections() {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=test;Password=test",
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
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("https://relay.example.test", provider.GetRequiredService<IOptions<MailRelayClientOptions>>().Value.BaseUrl);
        Assert.NotNull(provider.GetRequiredService<IMailRelayClient>());
    }

    [Fact]
    public void AddMailRelayTelemetry_WhenOtlpEndpointIsEmpty_RegistersMeterProvider() {
        var services = new ServiceCollection();

        services.AddSingleton<IOptions<OpenTelemetryOptions>>(Options.Create(new OpenTelemetryOptions {
            Otlp = new OpenTelemetryOptions.OtlpOptions {
                Endpoint = "",
            },
        }));
        services.AddMailRelayTelemetry();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<OpenTelemetry.Metrics.MeterProvider>());
    }
}
