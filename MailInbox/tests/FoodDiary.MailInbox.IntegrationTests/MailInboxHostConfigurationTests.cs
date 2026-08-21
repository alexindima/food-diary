using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using FoodDiary.MailInbox.WebApi;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.MailInbox.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxHostConfigurationTests {
    [Fact]
    public async Task ValidateRuntimeDatabaseRoleAsync_InDevelopment_ReturnsWithoutResolvingServices() {
        await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        await MailInboxHostConfiguration.ValidateRuntimeDatabaseRoleAsync(
            services,
            new ConfigurationBuilder().Build(),
            CreateEnvironment(Environments.Development),
            CancellationToken.None);
    }

    [Fact]
    public async Task ContainerHealthCheck_InvalidServerName_ReturnsFalse() {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["MailInboxSmtp:ServerName"] = "not a valid host name",
            })
            .Build();

        bool isReady = await MailInboxLocalTlsHealthCheck
            .IsReadyAsync(configuration["MailInboxSmtp:ServerName"], CancellationToken.None);

        Assert.False(isReady);
    }

    [Theory]
    [InlineData("Password=change-me-local-password")]
    [InlineData("Pwd=CHANGE-ME-LOCAL-PASSWORD")]
    public void Validate_ProductionPlaceholderPassword_Throws(string passwordSetting) {
        IConfiguration configuration = CreateConfiguration(
            $"Host=database;Database=mailbox;Username=postgres;{passwordSetting}");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("must not use the repository-local database password", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ProductionMissingConnectionString_Throws() {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("must be configured in Production", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ProductionMissingPassword_Throws() {
        IConfiguration configuration = CreateConfiguration(
            "Host=database;Database=mailbox;Username=postgres");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("must include a non-empty database password", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ProductionEmptyPassword_Throws() {
        IConfiguration configuration = CreateConfiguration(
            "Host=database;Database=mailbox;Username=postgres;Password=\"\"");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("must include a non-empty database password", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ProductionInvalidConnectionString_Throws() {
        IConfiguration configuration = CreateConfiguration("Host=database;Password=\"unterminated");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("must be a valid database connection string", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Password=strong-unique-production-password")]
    [InlineData("Pwd=another-strong-production-password")]
    public void Validate_ProductionStrongPassword_Succeeds(string passwordSetting) {
        IConfiguration configuration = CreateConfiguration(
            $"Host=database;Database=fooddiary_mailinbox;Username=mailinbox_runtime;{passwordSetting};SSL Mode=VerifyFull");

        MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production));
    }

    [Theory]
    [InlineData("postgres", "postgres", "must identify a dedicated mailinbox_* role")]
    [InlineData("mailinbox_initializer", "mailinbox_runtime", "Username must match")]
    public void Validate_ProductionUnsafeRuntimeRole_Throws(
        string connectionRoleName,
        string configuredRoleName,
        string expectedMessage) {
        IConfiguration configuration = CreateConfiguration(
            $"Host=database;Database=fooddiary_mailinbox;Username={connectionRoleName};Password=strong-unique-production-password;SSL Mode=VerifyFull",
            runtimeRoleName: configuredRoleName);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ProductionMatchingCustomRuntimeRole_Succeeds() {
        IConfiguration configuration = CreateConfiguration(
            "Host=database;Database=fooddiary_mailinbox;User ID=mailinbox_custom;Password=strong-unique-production-password;SSL Mode=VerifyFull",
            runtimeRoleName: "mailinbox_custom");

        MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production));
    }

    [Fact]
    public void Validate_ProductionSmtpWithoutTrustedRelay_Throws() {
        IConfiguration configuration = CreateConfiguration(
            "Host=database;Database=fooddiary_mailinbox;Username=mailinbox_runtime;Password=strong-unique-production-password;SSL Mode=VerifyFull",
            includeTrustedRelay: false);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("TrustedRelayNetworks", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Disable")]
    [InlineData("Prefer")]
    [InlineData("Require")]
    [InlineData("VerifyCA")]
    public void Validate_ProductionDatabaseTlsIsNotFullyVerified_Throws(string sslMode) {
        IConfiguration configuration = CreateConfiguration(
            $"Host=database;Database=fooddiary_mailinbox;Username=mailinbox_runtime;Password=strong-unique-production-password;SSL Mode={sslMode}");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("must use SSL Mode=VerifyFull", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ProductionWrongDatabase_Throws() {
        IConfiguration configuration = CreateConfiguration(
            "Host=database;Database=fooddiary;Username=postgres;Password=strong-unique-production-password");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("must target the dedicated 'fooddiary_mailinbox' database", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_DevelopmentPlaceholderPassword_Succeeds() {
        IConfiguration configuration = CreateConfiguration(
            "Host=localhost;Database=mailbox;Username=postgres;Password=change-me-local-password");

        MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Development));
    }

    private static IConfiguration CreateConfiguration(
        string connectionString,
        bool includeTrustedRelay = true,
        string runtimeRoleName = "mailinbox_runtime") {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["MailInboxRuntimeDatabase:RoleName"] = runtimeRoleName,
        };
        if (includeTrustedRelay) {
            values["MailInboxSmtp:TrustedRelayNetworks:0"] = "192.0.2.10/32";
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static IHostEnvironment CreateEnvironment(string environmentName) =>
        new TestHostEnvironment {
            EnvironmentName = environmentName,
        };

    private sealed class TestHostEnvironment : IHostEnvironment {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "FoodDiary.MailInbox.IntegrationTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
