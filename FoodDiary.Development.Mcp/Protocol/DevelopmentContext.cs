using FoodDiary.Development.Mcp.Wiki;

namespace FoodDiary.Development.Mcp.Protocol;

public sealed record DevelopmentContext(
    string SnapshotFingerprint,
    string GitHead,
    WikiCommandResult? ChangeContext,
    WikiCommandResult? BackendTrace,
    WikiCommandResult? TestPlan,
    bool PartialSuccess,
    IReadOnlyList<DevelopmentContextComponentError> ComponentErrors,
    IReadOnlyList<string> ExpandedScopePaths,
    bool ScopeMismatch,
    IReadOnlyList<string> EffectiveLayers,
    bool CrossLayerScope,
    string? BaseRevision = null,
    string? HeadRevision = null,
    bool BaselineAvailable = false,
    WikiContextSearchResult? SqlContextSearch = null,
    string ContextRetrievalSource = "sqlite",
    string? ContextFallbackReason = null,
    DevelopmentFlowScope? FlowScope = null) {
    public IReadOnlyList<string> SuggestedStartingPaths => ExpandedScopePaths.Take(3).ToArray();
    public IReadOnlyList<string> AdditionalCandidatePaths => ExpandedScopePaths.Skip(3).ToArray();
    public string ScopeInterpretation => "Ranked navigation candidates, not a confirmed edit scope or complete dependency chain. Read suggested starting paths first; additional candidates remain available for discovery and test planning.";

    public string RetrievalAssessment => SqlContextSearch switch {
        not { Ready: true, Fresh: true, Candidates.Count: > 0 } => "unavailable",
        { Candidates: var candidates } when candidates[0].Confidence is "low" or "unknown" => "low-confidence",
        { Candidates: var candidates } when candidates[0].Ambiguous => "ambiguous",
        _ => "ranked-candidates",
    };

    public string FlowCoverageAssessment => FlowScope switch {
        null => "not-assessed",
        { Ready: false } => "unavailable",
        { MissingStages.Count: > 0 } => "partial",
        _ => "bounded-evidence",
    };

    public IReadOnlyList<string> RetrievalWarnings {
        get {
            IReadOnlyList<string> warnings = RetrievalAssessment switch {
                "low-confidence" => ["The leading source candidate has low confidence. Refine the query or inspect the candidates; this does not establish whether the requested feature exists."],
                "ambiguous" => ["The leading source candidate is ambiguous. Inspect alternatives before choosing an edit scope."],
                "unavailable" => ["No fresh ranked source candidates are available. Inspect component errors or provide a planned path."],
                _ => [],
            };
            return FlowScope is { Ready: true, MissingStages.Count: > 0 } flow
                ? [.. warnings, $"The bounded flow scope has no source evidence for these stages: {string.Join(", ", flow.MissingStages)}. Continue discovery before choosing an edit scope."]
                : warnings;
        }
    }

    public DevelopmentContext WithoutRawOutput() => this with {
        ChangeContext = ChangeContext?.WithoutRawOutput(),
        BackendTrace = BackendTrace?.WithoutRawOutput(),
        TestPlan = TestPlan?.WithoutRawOutput(),
    };

    public DevelopmentContext ToCompact(bool includeRawOutput = false) => this with {
        ChangeContext = ChangeContext?.ToCompactChangeContext(includeRawOutput: includeRawOutput),
        BackendTrace = BackendTrace?.ToCompactTrace(includeRawOutput: includeRawOutput),
        TestPlan = TestPlan?.ToCompactTestPlan(includeRawOutput: includeRawOutput),
        ExpandedScopePaths = ExpandedScopePaths.Take(20).ToArray(),
        SqlContextSearch = SqlContextSearch?.ToCompact(),
        FlowScope = FlowScope is null ? null : FlowScope with {
            Paths = FlowScope.Paths.Take(20).ToArray(),
            Links = FlowScope.Links.Take(40).ToArray(),
            Truncated = FlowScope.Truncated || FlowScope.Links.Count > 40,
        },
    };
}
