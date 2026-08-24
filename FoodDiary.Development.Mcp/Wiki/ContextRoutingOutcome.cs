namespace FoodDiary.Development.Mcp.Wiki;

public enum ContextRoutingOutcome {
    SqlitePrimary = 0,
    SqliteUnavailable = 1,
    JsonFallback = 2,
}
