namespace FoodDiary.Development.Mcp.Tasks;

public sealed record TaskToolRequest(
    TaskToolAction Action,
    string? EvidencePath = null,
    string? CheckId = null,
    string? JobId = null,
    string? RuntimeName = null,
    int TimeoutMinutes = 20);
