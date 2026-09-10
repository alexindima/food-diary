using System.Text.Json;
using FoodDiary.Development.Mcp.Tools;
using ModelContextProtocol.Protocol;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class ToolExecutionTests {
    [Theory]
    [InlineData("low", false, true, "low-confidence")]
    [InlineData("unknown", false, true, "low-confidence")]
    [InlineData("medium", true, true, "ambiguous")]
    [InlineData("high", false, true, "ranked-candidates")]
    [InlineData("high", false, false, "unavailable")]
    public async Task RunToolAsync_ReportsRetrievalQualityWithoutClaimingFeatureAbsence(string confidence, bool ambiguous, bool fresh, string expected) {
        var search = new WikiContextSearchResult("sqlite", "reader", Ready: true, 1, "fingerprint", UpdatedAtUtc: null, "snapshot", "head", fresh, [],
            [new WikiContextSearchCandidate(1, "Candidate.cs", "code", "csharp", 10, 1, [], Confidence: confidence, Ambiguous: ambiguous)], 1);
        var context = new DevelopmentContext("snapshot", "head", ChangeContext: null, BackendTrace: null, TestPlan: null, PartialSuccess: false, [], ["Candidate.cs"], ScopeMismatch: false, [], CrossLayerScope: false, SqlContextSearch: search);
        CallToolResult result = await ToolExecution.RunToolAsync(() => Task.FromResult(context.ToCompact()), CancellationToken.None);
        JsonElement structured = Assert.IsType<JsonElement>(result.StructuredContent);
        TextContentBlock content = Assert.IsType<TextContentBlock>(Assert.Single(result.Content));
        Assert.Multiple(
            () => Assert.False(result.IsError),
            () => Assert.True(structured.GetProperty("success").GetBoolean()),
            () => Assert.Equal(expected, structured.GetProperty("data").GetProperty("retrievalAssessment").GetString()),
            () => Assert.Equal(context.RetrievalWarnings.Count > 0 ? context.RetrievalWarnings[0] : "Structured FoodDiary development context is available.", content.Text),
            () => Assert.False(context.PartialSuccess));
    }

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
