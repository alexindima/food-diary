namespace FoodDiary.Modules.Dashboard.Contracts.Models;

public sealed record DashboardHydrationEntryModel(Guid Id, DateTime TimestampUtc, int AmountMl);
