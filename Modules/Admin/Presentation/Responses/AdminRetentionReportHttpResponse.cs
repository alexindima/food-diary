namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminRetentionReportHttpResponse(DateTime FromUtc, DateTime ToUtc, DateTime AsOfUtc, int ActiveUsersInPeriod,
    IReadOnlyList<AdminRetentionCohortHttpResponse> Cohorts, IReadOnlyList<AdminRetentionDayHttpResponse> ActivityByDay, int MealEntriesInPeriod = 0, DateTime? CohortFromUtc = null, DateTime? CohortToUtc = null);
