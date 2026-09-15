namespace FoodDiary.Modules.Statistics.Application.Common;

internal sealed record LocalStatisticsDay(DateOnly Date, DateTime StartUtc, DateTime EndExclusiveUtc);
