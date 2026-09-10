using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Development.Mcp.Protocol;
using FoodDiary.Development.Mcp.Wiki;
using ModelContextProtocol.Protocol;

namespace FoodDiary.Development.Mcp.Tools;

public static class ToolExecution {
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

    public static async Task<string> RunJsonAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken) {
        DevelopmentMcpResult<T> result = await RunAsync(operation, cancellationToken).ConfigureAwait(false);
        return System.Text.Json.JsonSerializer.Serialize(result, JsonOptions);
    }

    public static async Task<DevelopmentMcpResult<T>> RunAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken) {
        try {
            return new DevelopmentMcpResult<T>(
                Success: true,
                Data: await operation().ConfigureAwait(false),
                ErrorCode: null,
                ErrorMessage: null);
        } catch (DevelopmentMcpException exception) {
            return Failure<T>(exception.ErrorCode, exception.Message);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            return Failure<T>(
                DevelopmentMcpErrorCodes.Cancelled,
                "The MCP tool call was cancelled.");
        } catch (Exception exception) {
            return Failure<T>(
                DevelopmentMcpErrorCodes.Unexpected,
                exception.Message);
        }
    }

    public static async Task<CallToolResult> RunToolAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken) {
        DevelopmentMcpResult<T> result = await RunAsync(operation, cancellationToken).ConfigureAwait(false);
        return new CallToolResult {
            StructuredContent = JsonSerializer.SerializeToElement(result, JsonOptions),
            IsError = !result.Success,
            Content = [new TextContentBlock {
                Text = result.Success
                    ? SuccessMessage(result.Data)
                    : $"{result.ErrorCode}: {result.ErrorMessage}",
            }],
        };
    }

    private static string SuccessMessage<T>(T data) =>
        data is WikiCommandResult { StructuredOutput: { ValueKind: JsonValueKind.Object } output } &&
        output.TryGetProperty("status", out JsonElement status) && string.Equals(status.GetString(), "no-match", StringComparison.Ordinal)
            ? "No matching indexed symbol was found. See warnings and nextSteps for scope and recovery."
            : "Structured FoodDiary development context is available.";

    private static DevelopmentMcpResult<T> Failure<T>(string errorCode, string errorMessage) =>
        new(
            Success: false,
            Data: default,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
}
