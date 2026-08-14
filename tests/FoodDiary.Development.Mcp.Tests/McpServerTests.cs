using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class McpServerTests {
    [Fact]
    public async Task ConfiguredServer_ListsAndCallsExpectedReadOnlyTools() {
        string repositoryRoot = FindRepositoryRoot();
        var configuration = CodexMcpTestConfiguration.Load(repositoryRoot);
        Assert.Contains("--no-build", configuration.Arguments, StringComparer.Ordinal);

        var transport = new StdioClientTransport(configuration.CreateTransportOptions("FoodDiary Development MCP test"));
        using CancellationTokenSource connectionTimeout = new(TimeSpan.FromSeconds(30));
        await using McpClient client = await McpClient.CreateAsync(
            transport,
            cancellationToken: connectionTimeout.Token);

        IList<McpClientTool> tools = await client.ListToolsAsync(
            cancellationToken: connectionTimeout.Token);

        string[] expected = ["get_change_context", "get_development_context", "get_server_status", "get_test_plan", "trace_backend_flow"];
        string[] actual = [.. tools
            .Select(tool => tool.Name)
            .Order(StringComparer.Ordinal)];
        Assert.True(expected.SequenceEqual(actual, StringComparer.Ordinal));
        Assert.True(expected.SequenceEqual(
            configuration.EnabledTools.Order(StringComparer.Ordinal),
            StringComparer.Ordinal));
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

    [Fact]
    public async Task ConfiguredServer_SupportsConcurrentClientsAndStatusCalls() {
        string repositoryRoot = FindRepositoryRoot();
        var configuration = CodexMcpTestConfiguration.Load(repositoryRoot);

        CallToolResult[] results = await Task.WhenAll(
            CallStatusAsync(configuration, "FoodDiary MCP concurrent client 1"),
            CallStatusAsync(configuration, "FoodDiary MCP concurrent client 2"));

        Assert.All(results, result => {
            Assert.NotEqual(true, result.IsError);
            Assert.Contains(result.Content, content =>
                content.ToString()!.Contains("repositoryRoot", StringComparison.OrdinalIgnoreCase));
        });
    }

    private static async Task<CallToolResult> CallStatusAsync(
        CodexMcpTestConfiguration configuration,
        string name) {
        var transport = new StdioClientTransport(configuration.CreateTransportOptions(name));
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(2));
        await using McpClient client = await McpClient.CreateAsync(
            transport,
            cancellationToken: timeout.Token);
        return await client.CallToolAsync(
            "get_server_status",
            cancellationToken: timeout.Token);
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
