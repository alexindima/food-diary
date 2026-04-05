using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Usda.Models;

namespace FoodDiary.Application.Usda.Queries.GetDailyMicronutrients;

public record GetDailyMicronutrientsQuery(
    Guid? UserId,
    DateTime Date) : IQuery<Result<DailyMicronutrientSummaryModel>>, IUserRequest;
