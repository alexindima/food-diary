namespace FoodDiary.Development.Mcp.Wiki;

public sealed record WikiCommandTiming(
    string Command,
    int Samples,
    double P50Milliseconds,
    double P95Milliseconds,
    double MaximumMilliseconds);
