namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminBugJournalPageHttpResponse(IReadOnlyList<AdminBugJournalEntryHttpResponse> Items, long TotalItems, bool IsConfigured) {
    public AdminAuditRunSummaryHttpResponse? LatestAudit { get; init; }
}
