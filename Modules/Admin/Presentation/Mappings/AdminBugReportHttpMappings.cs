using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminBugReports;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;

namespace FoodDiary.Modules.Admin.Presentation.Mappings;

public static class AdminBugReportHttpMappings {
    public static GetAdminBugReportsQuery ToQuery(this GetAdminBugReportsHttpQuery query) =>
        new(new AdminBugReportFilter(query.Page, query.Limit, query.FromUtc, query.ToUtc, query.Status, query.Search, query.Id, Source: "email"));
    public static GetAdminBugReportsQuery ToQuery(this GetAdminBugJournalHttpQuery query) =>
        new(new AdminBugReportFilter(query.Page, query.Limit, query.FromUtc, query.ToUtc, query.Status, query.Search, query.Id, query.Source));
    public static AdminBugReportPageHttpResponse ToHttpResponse(this AdminBugReportPage page) =>
        new(page.Items.Select(item => new AdminBugReportHttpResponse(item.Id,
            item.SourceMessageId ?? throw new InvalidOperationException("An email report requires a source message ID."), item.ReceivedAtUtc,
            item.Subject, item.Status, item.Attempt, item.Summary, item.MergeRequestUrl, item.ContentExpired)).ToList(), page.TotalItems, page.IsConfigured);
    public static AdminBugJournalPageHttpResponse ToJournalHttpResponse(this AdminBugReportPage page) =>
        new(page.Items.Select(item => new AdminBugJournalEntryHttpResponse(item.Id, item.SourceMessageId, item.ReceivedAtUtc, item.Subject,
            item.Status, item.Attempt, item.Summary, item.MergeRequestUrl, item.ContentExpired) {
            Source = item.Source,
            AuditRunId = item.AuditRunId,
            AuditFromUtc = item.AuditFromUtc,
            AuditToUtc = item.AuditToUtc,
        }).ToList(), page.TotalItems, page.IsConfigured) {
            LatestAudit = page.LatestAudit is { } audit ? new AdminAuditRunSummaryHttpResponse(audit.Id, audit.FromUtc, audit.ToUtc,
                audit.StartedAtUtc, audit.CompletedAtUtc, audit.Status, audit.Summary, audit.FindingCount, audit.ContentExpired,
                audit.IsStale, audit.LastSuccessfulAtUtc) : null,
        };
}
