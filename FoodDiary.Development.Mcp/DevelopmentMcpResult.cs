namespace FoodDiary.Development.Mcp;

public sealed record DevelopmentMcpResult<T>(
    bool Success,
    T? Data,
    string? ErrorCode,
    string? ErrorMessage,
    bool ReadOnly = true);
