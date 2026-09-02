using System.Text.Json;
using FoodDiary.Development.Mcp.Tools;
using ModelContextProtocol.Protocol;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class ToolExecutionTests {
    [Fact]
    public async Task RunAsync_WhenOperationSucceeds_ReturnsData() {
        DevelopmentMcpResult<int> result = await ToolExecution.RunAsync(
            () => Task.FromResult(42),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.Success),
            () => Assert.Equal(42, result.Data),
            () => Assert.True(result.ReadOnly),
            () => Assert.Null(result.ErrorCode));
    }

    [Fact]
    public async Task RunAsync_WhenKnownFailureOccurs_PreservesError() {
        DevelopmentMcpResult<int> result = await ToolExecution.RunAsync<int>(
            () => throw new DevelopmentMcpException("known", "known failure"),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.False(result.Success),
            () => Assert.Equal("known", result.ErrorCode),
            () => Assert.Equal("known failure", result.ErrorMessage));
    }

    [Fact]
    public async Task RunAsync_WhenCallerCancels_ReturnsCancelledFailure() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        DevelopmentMcpResult<int> result = await ToolExecution.RunAsync<int>(
            () => Task.FromCanceled<int>(cancellation.Token),
            cancellation.Token);

        Assert.Multiple(
            () => Assert.False(result.Success),
            () => Assert.Equal(DevelopmentMcpErrorCodes.Cancelled, result.ErrorCode));
    }

    [Fact]
    public async Task RunAsync_WhenUnexpectedFailureOccurs_ReturnsUnexpectedFailure() {
        DevelopmentMcpResult<int> result = await ToolExecution.RunAsync<int>(
            () => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.False(result.Success),
            () => Assert.Equal(DevelopmentMcpErrorCodes.Unexpected, result.ErrorCode),
            () => Assert.Equal("boom", result.ErrorMessage));
    }

    [Fact]
    public async Task RunJsonAsync_SerializesSuccessfulEnvelope() {
        string json = await ToolExecution.RunJsonAsync(
            () => Task.FromResult(new { value = 7 }),
            CancellationToken.None);

        using var document = JsonDocument.Parse(json);
        Assert.Multiple(
            () => Assert.True(document.RootElement.GetProperty("success").GetBoolean()),
            () => Assert.Equal(7, document.RootElement.GetProperty("data").GetProperty("value").GetInt32()),
            () => Assert.False(document.RootElement.TryGetProperty("errorCode", out _)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RunToolAsync_MapsEnvelopeToProtocolResult(bool succeed) {
        CallToolResult result = await ToolExecution.RunToolAsync(
            () => succeed
                ? Task.FromResult(7)
                : Task.FromException<int>(new InvalidOperationException("boom")),
            CancellationToken.None);

        JsonElement structured = Assert.IsType<JsonElement>(result.StructuredContent);
        TextContentBlock content = Assert.IsType<TextContentBlock>(Assert.Single(result.Content));
        Assert.Multiple(
            () => Assert.Equal(!succeed, result.IsError),
            () => Assert.Equal(succeed, structured.GetProperty("success").GetBoolean()),
            () => Assert.Contains(
                succeed ? "available" : DevelopmentMcpErrorCodes.Unexpected,
                content.Text,
                StringComparison.Ordinal));
    }
}
