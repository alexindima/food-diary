namespace FoodDiary.Development.Mcp.Wiki;

public sealed record WikiCommandStageTiming(
    string Command,
    string Stage,
    int Samples,
    double P50Milliseconds,
    double P95Milliseconds,
    double MaximumMilliseconds);
