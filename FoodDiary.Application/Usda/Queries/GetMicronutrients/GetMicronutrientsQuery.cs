using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Usda.Models;

namespace FoodDiary.Application.Usda.Queries.GetMicronutrients;

public record GetMicronutrientsQuery(int FdcId) : IQuery<Result<UsdaFoodDetailModel>>;
