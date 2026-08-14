namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class PowerShellWikiCommandExecutorTests {
    [Fact]
    public async Task ServerStatus_ReturnsWithoutRunningDeepVerification() {
        ServerStatusService service = new();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));

        ServerStatus result = await service.GetStatusAsync(timeout.Token);

        Assert.True(result.WikiAvailable);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFocusedTestPlan() {
        PowerShellWikiCommandExecutor executor = new();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));

        WikiCommandResult result = await executor.ExecuteAsync(
            "test-plan",
            [
                "-Intent",
                "Verify the FoodDiary Development MCP test plan tool",
                "-ChangedPathList",
                "FoodDiary.Development.Mcp/WikiQueryService.cs",
            ],
            timeout.Token);

        Assert.Contains("Test plan:", result.RawOutput, StringComparison.Ordinal);
    }
}
