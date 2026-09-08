namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminRetentionReport(DateTime FromUtc, DateTime ToUtc, DateTime AsOfUtc,
    int ActiveUsersInPeriod, IReadOnlyList<AdminRetentionCohort> Cohorts, IReadOnlyList<AdminRetentionDay> ActivityByDay);
