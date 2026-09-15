namespace FoodDiary.Modules.Dashboard.Application.Abstractions.Models;

public sealed record DashboardWaistSummaryReadModel(DateTime DateFrom, DateTime DateTo, double AverageCircumferenceCm);
