using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Usda.Models;

namespace FoodDiary.Application.Usda.Queries.GetMicronutrients;

public record GetMicronutrientsQuery(int FdcId) : IQuery<Result<UsdaFoodDetailModel>>;
