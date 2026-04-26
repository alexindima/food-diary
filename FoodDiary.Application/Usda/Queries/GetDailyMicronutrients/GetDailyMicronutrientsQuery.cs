using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Usda.Models;

namespace FoodDiary.Application.Usda.Queries.GetDailyMicronutrients;

public record GetDailyMicronutrientsQuery(
    Guid? UserId,
    DateTime Date) : IQuery<Result<DailyMicronutrientSummaryModel>>, IUserRequest;
