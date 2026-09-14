namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record MarketingAttributionDayHttpResponse(DateTime Date, int Visits, int Signups, int PremiumStarts);
