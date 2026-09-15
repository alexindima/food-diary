namespace FoodDiary.Modules.Marketing.Contracts.Models;

public sealed record MarketingAttributionDayModel(DateTime Date, int Visits, int Signups, int PremiumStarts);
