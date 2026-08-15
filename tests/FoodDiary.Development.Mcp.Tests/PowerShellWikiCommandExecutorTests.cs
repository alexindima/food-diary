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
                "-Format",
                "Json",
                "-Fast",
                "-Objective",
                "Verify the FoodDiary Development MCP test plan tool",
                "-ChangedPath",
                "FoodDiary.Development.Mcp/WikiQueryService.cs",
            ],
            timeout.Token);

        Assert.StartsWith("{", result.RawOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_UsesRequestFileForLongUnicodeScope() {
        PowerShellWikiCommandExecutor executor = new();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(60));
        List<string> arguments = ["-Format", "Json", "-Fast", "-Objective", new string('я', 12_000)];
        for (int index = 0; index < 250; index++) {
            arguments.Add("-ChangedPath");
            arguments.Add($"FoodDiary.Web.Client/src/app/измерения/компонент-{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}.ts");
        }

        WikiCommandResult result = await executor.ExecuteAsync(
            "test-plan",
            arguments,
            timeout.Token);

        Assert.StartsWith("{", result.RawOutput, StringComparison.Ordinal);
    }
}
