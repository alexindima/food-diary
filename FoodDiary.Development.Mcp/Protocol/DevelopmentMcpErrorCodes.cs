namespace FoodDiary.Development.Mcp.Protocol;

public static class DevelopmentMcpErrorCodes {
    public const string RepositoryNotFound = "repository_not_found";
    public const string WikiUnavailable = "wiki_unavailable";
    public const string WikiCommandFailed = "wiki_command_failed";
    public const string IndexStale = "index_stale";
    public const string ContextSearchUnavailable = "context_search_unavailable";
    public const string TestPlanScopeRequired = "test_plan_scope_required";
    public const string SnapshotChanged = "snapshot_changed";
    public const string InvalidRevisionRange = "invalid_revision_range";
    public const string Timeout = "timeout";
    public const string Cancelled = "cancelled";
    public const string Unexpected = "unexpected_error";
}
