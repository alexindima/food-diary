using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Usda.Contracts.Models;

namespace FoodDiary.Modules.Usda.Application.Queries.GetMicronutrients;

public record GetMicronutrientsQuery(int FdcId) : IQuery<Result<UsdaFoodDetailModel>>;
