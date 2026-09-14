namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminRetentionCohortHttpResponse(DateTime Date, int Registered, int? ActivatedWithinSevenDays, int? Day1, int? Day7, int? Day30);
