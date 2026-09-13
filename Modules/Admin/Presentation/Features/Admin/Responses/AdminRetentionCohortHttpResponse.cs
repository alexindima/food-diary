namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminRetentionCohortHttpResponse(DateTime Date, int Registered, int? ActivatedWithinSevenDays, int? Day1, int? Day7, int? Day30);
