namespace FoodDiary.Application.Statistics.Common;

internal sealed record LocalStatisticsDay(DateOnly Date, DateTime StartUtc, DateTime EndExclusiveUtc);
