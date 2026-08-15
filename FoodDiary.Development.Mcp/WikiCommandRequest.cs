namespace FoodDiary.Development.Mcp;

public sealed record WikiCommandRequest(
    int SchemaVersion,
    IReadOnlyDictionary<string, object> Arguments);
