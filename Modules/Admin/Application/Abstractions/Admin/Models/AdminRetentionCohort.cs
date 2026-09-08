namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminRetentionCohort(DateTime Date, int Registered, int? ActivatedWithinSevenDays, int? Day1, int? Day7, int? Day30);
