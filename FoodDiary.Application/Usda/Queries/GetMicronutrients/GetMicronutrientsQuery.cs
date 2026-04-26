using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Usda.Models;

namespace FoodDiary.Application.Usda.Queries.GetMicronutrients;

public record GetMicronutrientsQuery(int FdcId) : IQuery<Result<UsdaFoodDetailModel>>;
