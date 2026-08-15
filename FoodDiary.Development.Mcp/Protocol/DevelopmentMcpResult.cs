namespace FoodDiary.Development.Mcp.Protocol;

public sealed record DevelopmentMcpResult<T>(
    bool Success,
    T? Data,
    string? ErrorCode,
    string? ErrorMessage,
    bool ReadOnly = true);
