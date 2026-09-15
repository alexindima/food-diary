namespace FoodDiary.Modules.Marketing.Application.Abstractions.Common;

public sealed record MarketingAttributionDayRecord(DateTime Date, int Visits, int Signups, int PremiumStarts);
