using System.Text.Json;
using FoodDiary.Initializer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class InitializerConfigurationTests {
    [Theory]
    [InlineData("")]
    [InlineData("FOODDIARY_")]
    public void Configuration_EnvironmentOverridesRepositorySettings(string prefix) {
        using var files = new ConfigurationFiles();
        string connectionName = $"InitializerTest{Guid.NewGuid():N}";
        string environmentVariable = $"{prefix}ConnectionStrings__{connectionName}";
        files.WriteApiSettings("appsettings.json", connectionName, "repository-default");
        files.WriteApiSettings("appsettings.ArchitectureTest.json", connectionName, "environment-file-default");
        string? previous = Environment.GetEnvironmentVariable(environmentVariable);
        try {
            Environment.SetEnvironmentVariable(environmentVariable, "explicit-environment-target");
            HostApplicationBuilder builder = CreateBuilder("ArchitectureTest");

            global::Program.ConfigureConfiguration(builder, StatusCommand(), files.Root, taskConfiguration: null);

            Assert.Equal("explicit-environment-target", builder.Configuration.GetConnectionString(connectionName));
        } finally {
            Environment.SetEnvironmentVariable(environmentVariable, previous);
        }
    }

    [Fact]
    public void Configuration_EnvironmentSpecificRepositorySettingsOverrideBaseFallback() {
        using var files = new ConfigurationFiles();
        string connectionName = $"InitializerTest{Guid.NewGuid():N}";
        files.WriteApiSettings("appsettings.json", connectionName, "base-default");
        files.WriteApiSettings("appsettings.ArchitectureTest.json", connectionName, "environment-default");
        HostApplicationBuilder builder = CreateBuilder("ArchitectureTest");

        global::Program.ConfigureConfiguration(builder, StatusCommand(), files.Root, taskConfiguration: null);

        Assert.Equal("environment-default", builder.Configuration.GetConnectionString(connectionName));
    }

    [Fact]
    public void Configuration_ExplicitConnectionOptionOverridesOtherProviders() {
        using var files = new ConfigurationFiles();
        files.WriteApiSettings("appsettings.json", "DefaultConnection", "repository-default");
        HostApplicationBuilder builder = CreateBuilder("ArchitectureTest");
        var command = new InitializerCommand("status", TargetMigration: null, ConnectionString: "command-target");

        global::Program.ConfigureConfiguration(builder, command, files.Root, taskConfiguration: null);

        Assert.Equal("command-target", builder.Configuration.GetConnectionString("DefaultConnection"));
    }

    [Theory]
    [InlineData(null, "task-target")]
    [InlineData("command-target", "command-target")]
    public void Configuration_TaskFileIsIsolatedAndPreservesExplicitConnectionOption(string? connectionOption, string expected) {
        using var files = new ConfigurationFiles();
        files.WriteApiSettings("appsettings.json", "DefaultConnection", "repository-default");
        string taskPath = files.WriteTaskSettings("task-target");
        HostApplicationBuilder builder = CreateBuilder(Environments.Development);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["InheritedSetting"] = "must-be-removed",
        });
        var command = new InitializerCommand("status", TargetMigration: null, ConnectionString: connectionOption);

        global::Program.ConfigureConfiguration(builder, command, files.Root, taskPath);

        Assert.Multiple(
            () => Assert.Equal(expected, builder.Configuration.GetConnectionString("DefaultConnection")),
            () => Assert.Null(builder.Configuration["InheritedSetting"]));
    }

    private static HostApplicationBuilder CreateBuilder(string environmentName) => Host.CreateApplicationBuilder(
        new HostApplicationBuilderSettings {
            DisableDefaults = true,
            EnvironmentName = environmentName,
        });

    private static InitializerCommand StatusCommand() => new("status", TargetMigration: null, ConnectionString: null);

    [ExcludeFromCodeCoverage]
    private sealed class ConfigurationFiles : IDisposable {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), $"fd-initializer-config-{Guid.NewGuid():N}");

        public void WriteApiSettings(string fileName, string connectionName, string value) {
            string directory = Path.Combine(Root, "FoodDiary.Web.Api");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, fileName), SerializeConnection(connectionName, value));
        }

        public string WriteTaskSettings(string value) {
            Directory.CreateDirectory(Root);
            string path = Path.Combine(Root, "task.json");
            File.WriteAllText(path, SerializeConnection("DefaultConnection", value));
            return path;
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);

        private static string SerializeConnection(string name, string value) => JsonSerializer.Serialize(
            new { ConnectionStrings = new Dictionary<string, string>(StringComparer.Ordinal) { [name] = value } });
    }
}
