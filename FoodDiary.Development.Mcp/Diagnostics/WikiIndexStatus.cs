namespace FoodDiary.Development.Mcp.Diagnostics;

public sealed record WikiIndexStatus(
    string Path,
    bool Exists,
    DateTimeOffset? LastWriteTimeUtc);
