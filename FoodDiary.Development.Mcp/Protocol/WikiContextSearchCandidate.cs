namespace FoodDiary.Development.Mcp.Protocol;

public sealed record WikiContextSearchCandidate(
    int Rank,
    string Path,
    string RecordType,
    string Category,
    int Score,
    double LexicalRank,
    IReadOnlyList<string> Reasons);
