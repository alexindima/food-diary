using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminRetention;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Mappings;

public static class AdminRetentionHttpMappings {
    public static GetAdminRetentionQuery ToQuery(this GetAdminRetentionHttpQuery query) => new(query.From, query.To);
    public static AdminRetentionReportHttpResponse ToHttpResponse(this AdminRetentionReport report) =>
        new(report.FromUtc, report.ToUtc, report.AsOfUtc, report.ActiveUsersInPeriod,
            report.Cohorts.Select(row => new AdminRetentionCohortHttpResponse(row.Date, row.Registered, row.ActivatedWithinSevenDays, row.Day1, row.Day7, row.Day30)).ToList(),
            report.ActivityByDay.Select(row => new AdminRetentionDayHttpResponse(row.Date, row.ActiveUsers)).ToList());
}
