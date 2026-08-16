namespace FoodDiary.Development.Mcp.Protocol;

public sealed record DevelopmentContextComponentError(
    string Component,
    string ErrorCode,
    string Message);
