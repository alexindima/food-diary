using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Extensions;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxSmtpOptionsTests {
    [Theory]
    [InlineData("Host=database;Database=fooddiary_mailinbox;Username=postgres", true)]
    [InlineData("Host=database;Database=FOODDIARY_MAILINBOX;Username=postgres", true)]
    [InlineData("Host=database;Database=fooddiary;Username=postgres", false)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void DatabaseConfiguration_TargetsRequiredDatabase_ReturnsExpectedResult(
        string connectionString,
        bool expected) {
        Assert.Equal(expected, MailInboxDatabaseConfiguration.TargetsRequiredDatabase(connectionString));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("http://localhost:4317", true)]
    [InlineData("https://telemetry.example.com/v1/traces", true)]
    [InlineData("relative", false)]
    [InlineData("ftp://telemetry.example.com", false)]
    [InlineData("file:///tmp/telemetry", false)]
    [InlineData("https://user:secret@telemetry.example.com", false)]
    [InlineData("https://telemetry.example.com?token=secret", false)]
    [InlineData("https://telemetry.example.com/#fragment", false)]
    public void OpenTelemetryOptions_HasValidOtlpEndpoint_ReturnsExpectedResult(
        string? endpoint,
        bool expected) {
        var options = new OpenTelemetryOptions {
            Otlp = new OpenTelemetryOptions.OtlpOptions { Endpoint = endpoint },
        };

        Assert.Equal(expected, OpenTelemetryOptions.HasValidOtlpEndpoint(options));
    }

    [Fact]
    public void HasValidConfiguration_WhenValuesAreValid_ReturnsTrue() {
        var options = new MailInboxSmtpOptions {
            ServerName = "mail.fooddiary.club",
            Port = 2525,
            CertificatePath = "/certs/fullchain.pem",
            PrivateKeyPath = "/certs/privkey.pem",
            MaxMessageSizeBytes = 1024,
            AllowedRecipients = ["admin@fooddiary.club"],
        };

        Assert.True(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void HasValidConfiguration_WhenPortIsMaximumValidValue_ReturnsTrue() {
        var options = new MailInboxSmtpOptions {
            Enabled = false,
            Port = ushort.MaxValue,
        };

        Assert.True(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData("0.0.0.0", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("::", true)]
    [InlineData("::1", true)]
    [InlineData("localhost", false)]
    [InlineData("", false)]
    public void HasValidConfiguration_ValidatesListenAddress(string listenAddress, bool expected) {
        var options = new MailInboxSmtpOptions {
            Enabled = false,
            ListenAddress = listenAddress,
        };

        Assert.Equal(expected, MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData("", "/certs/privkey.pem")]
    [InlineData("/certs/fullchain.pem", "")]
    public void HasValidConfiguration_WhenEnabledCertificatePathIsMissing_ReturnsFalse(
        string certificatePath,
        string privateKeyPath) {
        var options = new MailInboxSmtpOptions {
            CertificatePath = certificatePath,
            PrivateKeyPath = privateKeyPath,
        };

        Assert.False(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void HasValidConfiguration_WhenDisabledCertificatePathsAreMissing_ReturnsTrue() {
        var options = new MailInboxSmtpOptions { Enabled = false };

        Assert.True(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData(0, 1024)]
    [InlineData(65536, 1024)]
    [InlineData(2525, 0)]
    public void HasValidConfiguration_WhenRequiredNumericValueIsInvalid_ReturnsFalse(
        int port,
        int maxMessageSizeBytes) {
        var options = new MailInboxSmtpOptions {
            ServerName = "mail.fooddiary.club",
            Port = port,
            MaxMessageSizeBytes = maxMessageSizeBytes,
            AllowedRecipients = ["admin@fooddiary.club"],
        };

        Assert.False(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void HasValidConfiguration_WhenRecipientsAreEmpty_ReturnsFalse() {
        var options = new MailInboxSmtpOptions {
            ServerName = "mail.fooddiary.club",
            Port = 2525,
            MaxMessageSizeBytes = 1024,
            AllowedRecipients = [],
        };

        Assert.False(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void HasValidConfiguration_WhenPerIpConnectionsExceedGlobalLimit_ReturnsFalse() {
        var options = new MailInboxSmtpOptions {
            MaxConcurrentConnections = 2,
            MaxConcurrentConnectionsPerIp = 3,
        };

        Assert.False(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void HasValidConfiguration_WhenPerIpByteBudgetIsSmallerThanOneMessage_ReturnsFalse() {
        var options = new MailInboxSmtpOptions {
            MaxMessageSizeBytes = 1024,
            MaxRawBytesPerIpPerHour = 1023,
        };

        Assert.False(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void StorageOptions_WhenMetadataRetentionIsShorterThanContentRetention_AreInvalid() {
        var options = new MailInboxStorageOptions {
            ContentRetention = TimeSpan.FromDays(30),
            MetadataRetention = TimeSpan.FromDays(7),
        };

        Assert.False(MailInboxStorageOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void StorageOptions_WhenMessageDetailConcurrencyIsZero_AreInvalid() {
        var options = new MailInboxStorageOptions { MaxConcurrentMessageDetailReads = 0 };

        Assert.False(MailInboxStorageOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void StorageOptions_WhenUntrustedQuotaDoesNotReserveCapacity_AreInvalid() {
        var options = new MailInboxStorageOptions {
            MaxMessagesPerDay = 10,
            MaxUntrustedMessagesPerDay = 10,
            MaxRawBytesPerDay = 1024,
            MaxUntrustedRawBytesPerDay = 1024,
        };

        Assert.False(MailInboxStorageOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData("192.0.2.10", true)]
    [InlineData("192.0.2.0/24", true)]
    [InlineData("2001:db8::/64", true)]
    [InlineData("0.0.0.0/0", false)]
    [InlineData("invalid", false)]
    public void HasValidConfiguration_ValidatesTrustedRelayNetworks(string network, bool expected) {
        var options = new MailInboxSmtpOptions {
            Enabled = false,
            TrustedRelayNetworks = [network],
        };

        Assert.Equal(expected, MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void HasValidConfiguration_WhenServerNameIsBlank_ReturnsFalse(string serverName) {
        var options = new MailInboxSmtpOptions {
            ServerName = serverName,
            Port = 2525,
            MaxMessageSizeBytes = 1024,
            AllowedRecipients = ["admin@fooddiary.club"],
        };

        Assert.False(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData("")]
    [InlineData("support")]
    public void HasValidConfiguration_WhenRecipientIsInvalid_ReturnsFalse(string recipient) {
        var options = new MailInboxSmtpOptions {
            ServerName = "mail.fooddiary.club",
            Port = 2525,
            MaxMessageSizeBytes = 1024,
            AllowedRecipients = [recipient],
        };

        Assert.False(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void AddMailInboxInfrastructure_BindsAndValidatesIngestionOptions() {
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary_mailinbox;Username=postgres;Password=test",
                ["MailInboxSmtp:Enabled"] = "false",
                ["MailInboxSmtp:ServerName"] = "mail.fooddiary.club",
                ["MailInboxSmtp:ListenAddress"] = "127.0.0.1",
                ["MailInboxSmtp:Port"] = "2526",
                ["MailInboxSmtp:MaxMessageSizeBytes"] = "4096",
                ["MailInboxSmtp:MaxRawBytesPerIpPerHour"] = "32768",
                ["MailInboxSmtp:AllowedRecipients:0"] = "admin@fooddiary.club",
                ["MailInboxStorage:MaxMessagesPerDay"] = "17",
                ["MailInboxStorage:MaxRawBytesPerDay"] = "65536",
                ["MailInboxStorage:DeduplicationWindow"] = "12:00:00",
                ["MailInboxStorage:ContentRetention"] = "14.00:00:00",
                ["MailInboxStorage:MetadataRetention"] = "90.00:00:00",
                ["MailInboxStorage:CleanupInterval"] = "03:00:00",
                ["MailInboxStorage:CleanupBatchSize"] = "25",
                ["MailInboxStorage:MaxConcurrentMessageDetailReads"] = "3",
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddMailInboxInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        MailInboxSmtpOptions options = provider.GetRequiredService<IOptions<MailInboxSmtpOptions>>().Value;
        Assert.False(options.Enabled);
        Assert.Equal("mail.fooddiary.club", options.ServerName);
        Assert.Equal("127.0.0.1", options.ListenAddress);
        Assert.Equal(2526, options.Port);
        Assert.Equal(4096, options.MaxMessageSizeBytes);
        Assert.Equal(32768, options.MaxRawBytesPerIpPerHour);
        Assert.Contains("admin@fooddiary.club", options.AllowedRecipients, StringComparer.Ordinal);

        MailInboxStorageOptions storageOptions = provider.GetRequiredService<IOptions<MailInboxStorageOptions>>().Value;
        Assert.Equal(17, storageOptions.MaxMessagesPerDay);
        Assert.Equal(65536, storageOptions.MaxRawBytesPerDay);
        Assert.Equal(TimeSpan.FromHours(12), storageOptions.DeduplicationWindow);
        Assert.Equal(TimeSpan.FromDays(14), storageOptions.ContentRetention);
        Assert.Equal(TimeSpan.FromDays(90), storageOptions.MetadataRetention);
        Assert.Equal(TimeSpan.FromHours(3), storageOptions.CleanupInterval);
        Assert.Equal(25, storageOptions.CleanupBatchSize);
        Assert.Equal(3, storageOptions.MaxConcurrentMessageDetailReads);
    }

    [Fact]
    public void AddMailInboxInfrastructure_WhenConnectionStringIsMissing_ThrowsOnDataSourceResolution() {
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["MailInboxSmtp:Enabled"] = "false",
                ["MailInboxSmtp:ServerName"] = "mail.fooddiary.club",
                ["MailInboxSmtp:Port"] = "2526",
                ["MailInboxSmtp:MaxMessageSizeBytes"] = "4096",
                ["MailInboxSmtp:AllowedRecipients:0"] = "admin@fooddiary.club",
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddMailInboxInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<NpgsqlDataSource>());
    }

    [Fact]
    public void AddMailInboxInfrastructure_RegistersNpgsqlDataSourceWithoutOpeningConnection() {
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary_mailinbox;Username=postgres;Password=test",
                ["MailInboxSmtp:Enabled"] = "false",
                ["MailInboxSmtp:ServerName"] = "mail.fooddiary.club",
                ["MailInboxSmtp:Port"] = "2526",
                ["MailInboxSmtp:MaxMessageSizeBytes"] = "4096",
                ["MailInboxSmtp:AllowedRecipients:0"] = "admin@fooddiary.club",
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddMailInboxInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        NpgsqlDataSource dataSource = provider.GetRequiredService<NpgsqlDataSource>();

        Assert.NotNull(dataSource);
        Assert.IsType<NpgsqlInboundMailStore>(provider.GetRequiredService<NpgsqlInboundMailStore>());
    }

    [Fact]
    public void AddMailInboxInfrastructure_RegistersInfrastructureServices() {
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary_mailinbox;Username=postgres;Password=test",
                ["MailInboxSmtp:Enabled"] = "false",
                ["MailInboxSmtp:ServerName"] = "mail.fooddiary.club",
                ["MailInboxSmtp:Port"] = "2526",
                ["MailInboxSmtp:MaxMessageSizeBytes"] = "4096",
                ["MailInboxSmtp:AllowedRecipients:0"] = "admin@fooddiary.club",
                ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4317",
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddMailInboxInfrastructure(configuration);
        services.AddMailInboxTelemetry(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        NpgsqlInboundMailStore store = provider.GetRequiredService<NpgsqlInboundMailStore>();
        Assert.Same(store, provider.GetRequiredService<IInboundMailStore>());
        Assert.Same(store, provider.GetRequiredService<IMailInboxSchemaInitializer>());
        Assert.IsType<NpgsqlMailInboxReadinessChecker>(provider.GetRequiredService<IMailInboxReadinessChecker>());
        Assert.IsType<DmarcReportParser>(provider.GetRequiredService<DmarcReportParser>());
        Assert.Same(
            provider.GetRequiredService<DmarcReportParser>(),
            provider.GetRequiredService<IMailInboxDmarcReportParser>());
        Assert.IsType<SmtpInboundMessageStore>(provider.GetRequiredService<SmtpInboundMessageStore>());
        Assert.IsType<MailInboxSlidingWindowRateLimiter>(provider.GetRequiredService<MailInboxSlidingWindowRateLimiter>());
        Assert.IsType<MailInboxMailboxFilter>(provider.GetRequiredService<MailInboxMailboxFilter>());
        Assert.IsType<MailInboxEndpointListenerFactory>(provider.GetRequiredService<MailInboxEndpointListenerFactory>());
        Assert.NotNull(provider.GetRequiredService<MeterProvider>());
        Assert.NotNull(provider.GetRequiredService<OpenTelemetry.Trace.TracerProvider>());

        IHostedService[] hostedServices = [.. provider.GetServices<IHostedService>()];
        Assert.DoesNotContain(hostedServices, static service => service.GetType().Name.Contains("SchemaInitializer", StringComparison.Ordinal));
        Assert.Contains(hostedServices, static service => service is MailInboxRetentionHostedService);
        Assert.Contains(hostedServices, static service => service is MailInboxSmtpHostedService);
    }

    [Fact]
    public void AddMailInboxTelemetry_WithoutEndpoint_DoesNotRegisterProviders() {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddMailInboxTelemetry(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Multiple(
            () => Assert.Null(provider.GetService<MeterProvider>()),
            () => Assert.Null(provider.GetService<OpenTelemetry.Trace.TracerProvider>()));
    }

    [Fact]
    public void AddMailInboxTelemetry_WhenEndpointIsInvalid_Throws() {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["OpenTelemetry:Otlp:Endpoint"] = "relative",
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddMailInboxTelemetry(configuration));

        Assert.Contains("HTTP(S) URI", exception.Message, StringComparison.Ordinal);
    }
}
