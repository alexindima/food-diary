namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminRetentionCohortHttpResponse(DateTime Date, int Registered, int? ActivatedWithinSevenDays, int? Day1, int? Day7, int? Day30);
