using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class McpServerTests {
    [Fact]
    public async Task Server_ListsExpectedReadOnlyTools() {
        string repositoryRoot = FindRepositoryRoot();
        StdioClientTransportOptions options = new() {
            Name = "FoodDiary Development MCP test",
            Command = "dotnet",
            Arguments = [
                "run",
                "--project",
                "FoodDiary.Development.Mcp/FoodDiary.Development.Mcp.csproj",
                "--no-launch-profile",
                "--no-build",
            ],
            WorkingDirectory = repositoryRoot,
            ShutdownTimeout = TimeSpan.FromSeconds(10),
        };

        StdioClientTransport transport = new(options);
        using CancellationTokenSource connectionTimeout = new(TimeSpan.FromSeconds(30));
        await using McpClient client = await McpClient.CreateAsync(
            transport,
            cancellationToken: connectionTimeout.Token);

        IList<McpClientTool> tools = await client.ListToolsAsync(
            cancellationToken: connectionTimeout.Token);

        string[] expected = ["get_change_context", "get_test_plan", "trace_backend_flow"];
        string[] actual = [.. tools
            .Select(tool => tool.Name)
            .Order(StringComparer.Ordinal)];
        Assert.True(expected.SequenceEqual(actual, StringComparer.Ordinal));
        Assert.All(tools, tool => Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint));
        Assert.Contains("Use these tools first", client.ServerInstructions, StringComparison.Ordinal);

        using CancellationTokenSource toolTimeout = new(TimeSpan.FromMinutes(2));
        CallToolResult result = await client.CallToolAsync(
            "get_test_plan",
            new Dictionary<string, object?>(StringComparer.Ordinal) {
                ["intent"] = "Verify the FoodDiary Development MCP test plan tool",
            },
            cancellationToken: toolTimeout.Token);

        Assert.NotEqual(true, result.IsError);
        Assert.Contains(result.Content, content =>
            content.ToString()!.Contains("Test plan:", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            if (File.Exists(Path.Combine(directory.FullName, ".llm-wiki", "wiki.ps1"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found from the test output directory.");
    }
}
