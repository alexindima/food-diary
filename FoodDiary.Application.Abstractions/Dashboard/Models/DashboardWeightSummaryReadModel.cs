namespace FoodDiary.Application.Abstractions.Dashboard.Models;

public sealed record DashboardWeightSummaryReadModel(DateTime DateFrom, DateTime DateTo, double AverageWeight);
