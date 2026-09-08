using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetAdminRetention;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminRetentionHttpMappings {
    public static GetAdminRetentionQuery ToQuery(this GetAdminRetentionHttpQuery query) => new(query.From, query.To);
    public static AdminRetentionReportHttpResponse ToHttpResponse(this AdminRetentionReport report) =>
        new(report.FromUtc, report.ToUtc, report.AsOfUtc, report.ActiveUsersInPeriod,
            report.Cohorts.Select(row => new AdminRetentionCohortHttpResponse(row.Date, row.Registered, row.ActivatedWithinSevenDays, row.Day1, row.Day7, row.Day30)).ToList(),
            report.ActivityByDay.Select(row => new AdminRetentionDayHttpResponse(row.Date, row.ActiveUsers)).ToList());
}
