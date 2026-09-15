using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Usda.Contracts.Models;

namespace FoodDiary.Modules.Usda.Application.Queries.GetDailyMicronutrients;

public record GetDailyMicronutrientsQuery(
    Guid? UserId,
    DateTime Date) : IQuery<Result<DailyMicronutrientSummaryModel>>, IUserRequest;
