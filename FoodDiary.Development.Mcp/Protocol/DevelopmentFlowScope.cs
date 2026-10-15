namespace FoodDiary.Development.Mcp.Protocol;

public sealed record DevelopmentFlowScope(
    bool Ready,
    IReadOnlyList<string> Paths,
    IReadOnlyList<DevelopmentFlowLink> Links,
    IReadOnlyList<string> MissingStages,
    bool Truncated,
    double DurationMilliseconds,
    string? UnavailableReason = null) {
    public string Interpretation => "Bounded lexical and indexed relationship evidence; discovered stages do not prove a complete runtime chain. Inspect links and missing stages in authoritative sources.";
}
