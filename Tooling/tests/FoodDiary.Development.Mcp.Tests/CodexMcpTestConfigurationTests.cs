using System.Text.Json;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class CodexMcpTestConfigurationTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Load_PortableLauncherUsesTheTestCheckoutInsteadOfThePinnedWorkstation(bool usePortableLauncher) {
        DirectoryInfo checkout = Directory.CreateTempSubdirectory("fooddiary-mcp-config-");
        try {
            string configurationDirectory = Path.Combine(checkout.FullName, ".codex");
            Directory.CreateDirectory(configurationDirectory);
            string pinnedWorkingDirectory = Path.Combine(Path.GetTempPath(), "fooddiary-pinned-workstation");
            File.WriteAllText(Path.Combine(configurationDirectory, "config.toml"), $$"""
                [mcp_servers.fooddiary_development]
                command = "dotnet"
                args = ["pinned-server.dll"]
                cwd = {{JsonSerializer.Serialize(pinnedWorkingDirectory)}}
                enabled_tools = ["get_server_status"]
                required = false
                """);

            var configuration = CodexMcpTestConfiguration.Load(checkout.FullName, usePortableLauncher);

            string expectedDirectory = usePortableLauncher ? checkout.FullName : pinnedWorkingDirectory;
            Assert.Equal(Path.GetFullPath(expectedDirectory), configuration.WorkingDirectory);
            Assert.Equal(!usePortableLauncher, configuration.UsesConfiguredLauncher);
            Assert.Equal(configuration.WorkingDirectory, configuration.CreateTransportOptions("portable cwd regression").WorkingDirectory);
        } finally {
            checkout.Delete(recursive: true);
        }
    }
}
