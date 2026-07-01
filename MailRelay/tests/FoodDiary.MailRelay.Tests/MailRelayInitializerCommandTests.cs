extern alias Initializer;

using FoodDiary.MailRelay.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Initializer::FoodDiary.MailRelay.Initializer;

namespace FoodDiary.MailRelay.Tests;

public sealed class MailRelayInitializerCommandTests {
    [Fact]
    public void Parse_ReturnsNull_WhenArgsAreEmpty() {
        var command = InitializerCommand.Parse([]);

        Assert.Null(command);
    }

    [Fact]
    public void Parse_ReturnsCommandWithoutConnectionString_WhenOnlyNameProvided() {
        var command = InitializerCommand.Parse(["status"]);

        Assert.NotNull(command);
        Assert.Equal("status", command.Name);
        Assert.Null(command.ConnectionString);
    }

    [Theory]
    [InlineData("--connection-string")]
    [InlineData("-c")]
    public void Parse_ReturnsCommandWithConnectionString_WhenConnectionStringOptionProvided(string option) {
        var command = InitializerCommand.Parse(["update", option, "Host=localhost"]);

        Assert.NotNull(command);
        Assert.Equal("update", command.Name);
        Assert.Equal("Host=localhost", command.ConnectionString);
    }

    [Theory]
    [InlineData("--connection-string")]
    [InlineData("-c")]
    public void Parse_Throws_WhenConnectionStringValueIsMissing(string option) {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => InitializerCommand.Parse(["update", option]));

        Assert.Equal("Missing value for --connection-string.", exception.Message);
    }

    [Fact]
    public void Parse_Throws_WhenUnexpectedSecondCommandNameProvided() {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => InitializerCommand.Parse(["status", "update"]));

        Assert.Equal("Unexpected argument 'update'.", exception.Message);
    }

    [Fact]
    public void Parse_ReturnsNull_WhenOnlyConnectionStringOptionProvided() {
        var command = InitializerCommand.Parse(["--connection-string", "Host=localhost"]);

        Assert.Null(command);
    }

    [Fact]
    public void AddMailRelayInitializerServices_ResolvesSchemaInitializer() {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["MailRelayQueue:PollIntervalSeconds"] = "1",
                ["MailRelayQueue:BatchSize"] = "2",
                ["MailRelayQueue:MaxAttempts"] = "3",
                ["MailRelayQueue:BaseRetryDelaySeconds"] = "5",
                ["MailRelayQueue:MaxRetryDelaySeconds"] = "10",
                ["MailRelayQueue:LockTimeoutSeconds"] = "20",
            })
            .Build();
        var services = new ServiceCollection();

        services.AddMailRelayInitializerServices(
            configuration,
            "Host=localhost;Database=fooddiary;Username=test;Password=test");
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IMailRelaySchemaInitializer>());
    }
}
