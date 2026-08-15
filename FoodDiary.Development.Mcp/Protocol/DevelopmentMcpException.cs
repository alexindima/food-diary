namespace FoodDiary.Development.Mcp.Protocol;

public sealed class DevelopmentMcpException(string errorCode, string message, Exception? innerException = null)
    : Exception(message, innerException) {
    public string ErrorCode { get; } = errorCode;
}
