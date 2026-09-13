using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminBugReports;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Mappings;

public static class AdminBugReportHttpMappings {
    public static GetAdminBugReportsQuery ToQuery(this GetAdminBugReportsHttpQuery query) =>
        new(new AdminBugReportFilter(query.Page, query.Limit, query.FromUtc, query.ToUtc, query.Status, query.Search, query.Id));
    public static AdminBugReportPageHttpResponse ToHttpResponse(this AdminBugReportPage page) =>
        new(page.Items.Select(item => new AdminBugReportHttpResponse(item.Id, item.SourceMessageId, item.ReceivedAtUtc, item.Subject,
            item.Status, item.Attempt, item.Summary, item.MergeRequestUrl, item.ContentExpired)).ToList(), page.TotalItems, page.IsConfigured);
}
