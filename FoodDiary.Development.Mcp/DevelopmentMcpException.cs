namespace FoodDiary.Development.Mcp;

public sealed class DevelopmentMcpException(string errorCode, string message, Exception? innerException = null)
    : Exception(message, innerException) {
    public string ErrorCode { get; } = errorCode;
}
