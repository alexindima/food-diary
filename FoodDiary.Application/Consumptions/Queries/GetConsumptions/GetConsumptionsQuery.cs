using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Models;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptions;

public record GetConsumptionsQuery(
    Guid? UserId,
    int Page,
    int Limit,
    DateTime? DateFrom,
    DateTime? DateTo,
    IReadOnlyCollection<string>? MealTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null,
    bool? HasAiSession = null) : IQuery<Result<PagedResponse<ConsumptionModel>>>, IUserRequest;
