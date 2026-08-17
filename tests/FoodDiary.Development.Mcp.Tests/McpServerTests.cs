using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Diagnostics;
using System.Text.Json;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class McpServerTests {
    [Fact]
    public async Task ConfiguredServer_ListsAndCallsExpectedReadOnlyTools() {
        string repositoryRoot = FindRepositoryRoot();
        var configuration = CodexMcpTestConfiguration.Load(repositoryRoot);

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
        Assert.True(configuration.Required);
        Assert.Equal(["--build-if-missing"], configuration.Arguments);
        Assert.All(tools, tool => Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint));
        Assert.Contains(
            "includeDetailedContext",
            tools.Single(tool => string.Equals(tool.Name, "get_change_context", StringComparison.Ordinal)).ProtocolTool.InputSchema.ToString(),
            StringComparison.Ordinal);
        Assert.Contains("Use these tools first", client.ServerInstructions, StringComparison.Ordinal);

        using CancellationTokenSource toolTimeout = new(TimeSpan.FromMinutes(2));
        CallToolResult result = await client.CallToolAsync(
            "get_server_status",
            cancellationToken: toolTimeout.Token);

        Assert.False(result.IsError is true, JsonSerializer.Serialize(result));
        Assert.NotNull(result.StructuredContent);
        Assert.Contains(
            "repositoryRoot",
            result.StructuredContent.Value.ToString(),
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "runtimeIdentity",
            result.StructuredContent.Value.ToString(),
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "repositoryHeadAtStartup",
            result.StructuredContent.Value.ToString(),
            StringComparison.OrdinalIgnoreCase);
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
            Assert.NotNull(result.StructuredContent);
            Assert.Contains(
                "repositoryRoot",
                result.StructuredContent.Value.ToString(),
                StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ConfiguredServer_AggregatesContextWithoutLockingBuildOutput() {
        string repositoryRoot = FindRepositoryRoot();
        var configuration = CodexMcpTestConfiguration.Load(repositoryRoot);
        var transport = new StdioClientTransport(configuration.CreateTransportOptions("FoodDiary MCP aggregate test"));
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(90));
        await using McpClient client = await McpClient.CreateAsync(
            transport,
            cancellationToken: timeout.Token);

        var stopwatch = Stopwatch.StartNew();
        CallToolResult result = await client.CallToolAsync(
            "get_development_context",
            new Dictionary<string, object?>(StringComparer.Ordinal) {
                ["intent"] = "Improve FoodDiary Development MCP latency",
                ["query"] = "WikiQueryService GetDevelopmentContextAsync",
            },
            cancellationToken: timeout.Token);
        stopwatch.Stop();

        Assert.False(result.IsError is true, JsonSerializer.Serialize(result));
        Assert.NotNull(result.StructuredContent);
        Assert.Contains(
            "snapshotFingerprint",
            result.StructuredContent.Value.ToString(),
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "rawOutput",
            result.StructuredContent.Value.ToString(),
            StringComparison.OrdinalIgnoreCase);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(60), $"Aggregate context took {stopwatch.Elapsed}.");

        ProcessResult build = await RunProcessAsync(
            "dotnet",
            ["build", "FoodDiary.Development.Mcp/FoodDiary.Development.Mcp.csproj", "--no-restore"],
            repositoryRoot,
            timeout.Token);
        Assert.True(build.ExitCode == 0, build.Output);
    }

    [Fact]
    public async Task ConfiguredServer_ReturnsCompactChangeContextByDefault() {
        string repositoryRoot = FindRepositoryRoot();
        var configuration = CodexMcpTestConfiguration.Load(repositoryRoot);
        var transport = new StdioClientTransport(configuration.CreateTransportOptions("FoodDiary MCP compact context test"));
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(2));
        await using McpClient client = await McpClient.CreateAsync(
            transport,
            cancellationToken: timeout.Token);

        CallToolResult result = await client.CallToolAsync(
            "get_change_context",
            new Dictionary<string, object?>(StringComparer.Ordinal) {
                ["intent"] = "Summarize the current FoodDiary measurement display changes",
                ["plannedPath"] = "FoodDiary.Web.Client/src/app",
            },
            cancellationToken: timeout.Token);

        Assert.NotEqual(true, result.IsError);
        JsonElement structuredContent = result.StructuredContent!.Value;
        JsonElement data = structuredContent.GetProperty("data");
        JsonElement summary = data.GetProperty("structuredOutput");
        Assert.True(summary.GetProperty("compact").GetBoolean());
        Assert.False(summary.TryGetProperty("rolloutPlan", out _));
        Assert.True(structuredContent.GetRawText().Length < 30_000);
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

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken) {
        using Process process = new() {
            StartInfo = new ProcessStartInfo {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        foreach (string argument in arguments) {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return new ProcessResult(
            process.ExitCode,
            string.Join(Environment.NewLine, await standardOutput, await standardError));
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
