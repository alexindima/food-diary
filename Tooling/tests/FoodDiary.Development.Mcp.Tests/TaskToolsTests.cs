using System.Text.Json;
using FoodDiary.Development.Mcp.Tasks;
using FoodDiary.Development.Mcp.Tools;
using ModelContextProtocol.Protocol;
using NSubstitute;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class TaskToolsTests {
    [Fact]
    public async Task NodeExecutor_SerializesTypedOperationAndRejectsAnUnknownCheckBeforeLaunch() {
        string repositoryRoot = FoodDiary.Development.Mcp.Infrastructure.RepositoryRootResolver.Resolve();
        string relativePath = $".artifacts/llm-wiki/mcp-task-fixture-{Guid.NewGuid():N}.json";
        string absolutePath = Path.Combine(repositoryRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        await File.WriteAllTextAsync(absolutePath, JsonSerializer.Serialize(new {
            git = new { @base = new string('a', 40) },
            change = new { changedPaths = Array.Empty<string>() },
            checks = Array.Empty<object>(),
        }));
        try {
            NodeTaskToolExecutor executor = new();
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
            JsonElement next = await executor.ExecuteAsync(new TaskToolRequest(TaskToolAction.Next, EvidencePath: relativePath), timeout.Token);
            Assert.Equal("checks-resolved", next.GetProperty("state").GetString());
            FoodDiary.Development.Mcp.Protocol.DevelopmentMcpException failure = await Assert.ThrowsAsync<FoodDiary.Development.Mcp.Protocol.DevelopmentMcpException>(() =>
                executor.ExecuteAsync(new TaskToolRequest(TaskToolAction.Verify, EvidencePath: relativePath, CheckId: "arbitrary-command"), timeout.Token));
            Assert.Contains("check ID", failure.Message, StringComparison.Ordinal);
        } finally { File.Delete(absolutePath); }
    }

    [Fact]
    public async Task Verification_StartIsExplicitWriteAndRetainsItsJobIdentity() {
        ITaskToolExecutor executor = Substitute.For<ITaskToolExecutor>();
        executor.ExecuteAsync(Arg.Any<TaskToolRequest>(), Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.SerializeToElement(new { jobId = "owned-job", status = "queued" }));
        TaskTools tools = new(executor);
        CallToolResult result = await tools.VerifyTaskAsync(".artifacts/llm-wiki/task.json", "architecture-tests", 3);
        Assert.False(result.IsError);
        Assert.NotNull(result.StructuredContent);
        Assert.Multiple(
            () => Assert.False(result.StructuredContent.Value.GetProperty("readOnly").GetBoolean()),
            () => Assert.Equal("owned-job", result.StructuredContent.Value.GetProperty("data").GetProperty("jobId").GetString()));
        await executor.Received(1).ExecuteAsync(Arg.Is<TaskToolRequest>(request => request.Action == TaskToolAction.Verify && request.CheckId == "architecture-tests" && request.TimeoutMinutes == 3), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Diagnostics_UsesOwnedRuntimeNameAndReadOnlyEnvelope() {
        ITaskToolExecutor executor = Substitute.For<ITaskToolExecutor>();
        executor.ExecuteAsync(Arg.Any<TaskToolRequest>(), Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.SerializeToElement(new { status = "not-observed" }));
        TaskTools tools = new(executor);
        CallToolResult result = await tools.GetTaskDiagnosticsAsync(runtimeName: "fixture");
        Assert.NotNull(result.StructuredContent);
        Assert.True(result.StructuredContent.Value.GetProperty("readOnly").GetBoolean());
        await executor.Received(1).ExecuteAsync(Arg.Is<TaskToolRequest>(request => request.Action == TaskToolAction.Diagnostics && request.RuntimeName == "fixture"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelFailure_DoesNotMislabelItsEnvelopeAsReadOnly() {
        CallToolResult result = await ToolExecution.RunToolAsync<int>(() => throw new InvalidOperationException("Unknown owned job"), CancellationToken.None, readOnly: false);
        Assert.True(result.IsError);
        Assert.NotNull(result.StructuredContent);
        Assert.False(result.StructuredContent.Value.GetProperty("readOnly").GetBoolean());
    }
}
