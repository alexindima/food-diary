namespace FoodDiary.Development.Mcp.Wiki;

public sealed record WikiCommandRequest(
    int SchemaVersion,
    IReadOnlyDictionary<string, object> Arguments);
