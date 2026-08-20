using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using FoodDiary.MailInbox.WebApi;

namespace FoodDiary.MailInbox.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxHostConfigurationTests {
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
            $"Host=database;Database=mailbox;Username=postgres;{passwordSetting}");

        MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Production));
    }

    [Fact]
    public void Validate_DevelopmentPlaceholderPassword_Succeeds() {
        IConfiguration configuration = CreateConfiguration(
            "Host=localhost;Database=mailbox;Username=postgres;Password=change-me-local-password");

        MailInboxHostConfiguration.Validate(configuration, CreateEnvironment(Environments.Development));
    }

    private static IConfiguration CreateConfiguration(string connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
            })
            .Build();

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
