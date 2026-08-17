namespace FoodDiary.Development.Mcp.Wiki;

public sealed record WikiQueryCacheMetrics(
    int Entries,
    long Hits,
    long Misses,
    double HitRate);
