namespace FoodDiary.Presentation.Api.Features.Consumptions.Requests;

public sealed record GetConsumptionsHttpQuery(
    int Page = 1,
    int Limit = 10,
    DateTime? DateFrom = null,
    DateTime? DateTo = null);
