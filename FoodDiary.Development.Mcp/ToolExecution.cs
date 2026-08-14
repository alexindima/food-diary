namespace FoodDiary.Development.Mcp;

public static class ToolExecution {
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

    private static DevelopmentMcpResult<T> Failure<T>(string errorCode, string errorMessage) =>
        new(
            Success: false,
            Data: default,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
}
