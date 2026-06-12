using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Extensions;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FoodDiary.MailInbox.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxSmtpOptionsTests {
    [Fact]
    public void HasValidConfiguration_WhenValuesAreValid_ReturnsTrue() {
        var options = new MailInboxSmtpOptions {
            ServerName = "mail.fooddiary.club",
            Port = 2525,
            MaxMessageSizeBytes = 1024,
            AllowedRecipients = ["admin@fooddiary.club"],
        };

        Assert.True(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData(0, 1024)]
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
    public void AddMailInboxInfrastructure_BindsAndValidatesSmtpOptions() {
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

        services.AddMailInboxInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        MailInboxSmtpOptions options = provider.GetRequiredService<IOptions<MailInboxSmtpOptions>>().Value;
        Assert.False(options.Enabled);
        Assert.Equal("mail.fooddiary.club", options.ServerName);
        Assert.Equal(2526, options.Port);
        Assert.Equal(4096, options.MaxMessageSizeBytes);
        Assert.Contains("admin@fooddiary.club", options.AllowedRecipients);
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
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddMailInboxInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        NpgsqlInboundMailStore store = provider.GetRequiredService<NpgsqlInboundMailStore>();
        Assert.Same(store, provider.GetRequiredService<IInboundMailStore>());
        Assert.Same(store, provider.GetRequiredService<IMailInboxSchemaInitializer>());
        Assert.IsType<NpgsqlMailInboxReadinessChecker>(provider.GetRequiredService<IMailInboxReadinessChecker>());
        Assert.IsType<DmarcReportParser>(provider.GetRequiredService<DmarcReportParser>());
        Assert.IsType<SmtpInboundMessageStore>(provider.GetRequiredService<SmtpInboundMessageStore>());
        Assert.IsType<MailInboxMailboxFilter>(provider.GetRequiredService<MailInboxMailboxFilter>());

        IHostedService[] hostedServices = [.. provider.GetServices<IHostedService>()];
        Assert.Contains(hostedServices, static service => service is MailInboxSchemaInitializerHostedService);
        Assert.Contains(hostedServices, static service => service is MailInboxSmtpHostedService);
    }
}
