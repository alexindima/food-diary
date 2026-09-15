namespace FoodDiary.Modules.Dashboard.Application.Abstractions.Models;

public sealed record DashboardWeightSummaryReadModel(DateTime DateFrom, DateTime DateTo, double AverageWeightKg);
