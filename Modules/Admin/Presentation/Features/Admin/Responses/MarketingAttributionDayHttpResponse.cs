namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record MarketingAttributionDayHttpResponse(DateTime Date, int Visits, int Signups, int PremiumStarts);
