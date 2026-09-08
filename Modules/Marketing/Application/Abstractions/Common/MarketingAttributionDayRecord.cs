namespace FoodDiary.Application.Abstractions.Marketing.Common;

public sealed record MarketingAttributionDayRecord(DateTime Date, int Visits, int Signups, int PremiumStarts);
