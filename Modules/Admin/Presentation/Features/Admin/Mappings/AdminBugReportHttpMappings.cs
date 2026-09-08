using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetAdminBugReports;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminBugReportHttpMappings {
    public static GetAdminBugReportsQuery ToQuery(this GetAdminBugReportsHttpQuery query) =>
        new(new AdminBugReportFilter(query.Page, query.Limit, query.FromUtc, query.ToUtc, query.Status, query.Search, query.Id));
    public static AdminBugReportPageHttpResponse ToHttpResponse(this AdminBugReportPage page) =>
        new(page.Items.Select(item => new AdminBugReportHttpResponse(item.Id, item.SourceMessageId, item.ReceivedAtUtc, item.Subject,
            item.Status, item.Attempt, item.Summary, item.MergeRequestUrl, item.ContentExpired)).ToList(), page.TotalItems, page.IsConfigured);
}
