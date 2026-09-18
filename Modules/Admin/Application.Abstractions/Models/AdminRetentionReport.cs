namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminRetentionReport(DateTime FromUtc, DateTime ToUtc, DateTime AsOfUtc,
    int ActiveUsersInPeriod, IReadOnlyList<AdminRetentionCohort> Cohorts, IReadOnlyList<AdminRetentionDay> ActivityByDay, int MealEntriesInPeriod = 0, DateTime? CohortFromUtc = null, DateTime? CohortToUtc = null);
