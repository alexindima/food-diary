namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminRetentionReportHttpResponse(DateTime FromUtc, DateTime ToUtc, DateTime AsOfUtc, int ActiveUsersInPeriod,
    IReadOnlyList<AdminRetentionCohortHttpResponse> Cohorts, IReadOnlyList<AdminRetentionDayHttpResponse> ActivityByDay);
