namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminRetentionCohort(DateTime Date, int Registered, int? ActivatedWithinSevenDays, int? Day1, int? Day7, int? Day30);
