namespace FoodDiary.Development.Mcp.Protocol;

public sealed record WikiContextSearchCandidate(
    int Rank,
    string Path,
    string RecordType,
    string Category,
    int Score,
    double LexicalRank,
    IReadOnlyList<string> Reasons,
    int? ScoreMargin = null,
    string Confidence = "unknown",
    bool Ambiguous = false,
    string? AmbiguityReason = null,
    int SameNameCandidateCount = 1);
