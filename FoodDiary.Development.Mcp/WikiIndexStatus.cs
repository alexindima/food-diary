namespace FoodDiary.Development.Mcp;

public sealed record WikiIndexStatus(
    string Path,
    bool Exists,
    DateTimeOffset? LastWriteTimeUtc);
