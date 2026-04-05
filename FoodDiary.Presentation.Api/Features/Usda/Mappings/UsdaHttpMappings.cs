using FoodDiary.Application.Usda.Commands.LinkProductToUsdaFood;
using FoodDiary.Application.Usda.Commands.UnlinkProductFromUsdaFood;
using FoodDiary.Application.Usda.Models;
using FoodDiary.Application.Usda.Queries.GetDailyMicronutrients;
using FoodDiary.Application.Usda.Queries.GetMicronutrients;
using FoodDiary.Application.Usda.Queries.SearchUsdaFoods;
using FoodDiary.Presentation.Api.Features.Usda.Requests;
using FoodDiary.Presentation.Api.Features.Usda.Responses;

namespace FoodDiary.Presentation.Api.Features.Usda.Mappings;

public static class UsdaHttpMappings {
    public static SearchUsdaFoodsQuery ToQuery(string search, int limit) =>
        new(search, limit);

    public static GetMicronutrientsQuery ToQuery(int fdcId) =>
        new(fdcId);

    public static LinkProductToUsdaFoodCommand ToCommand(
        this LinkProductToUsdaFoodHttpRequest request, Guid userId, Guid productId) =>
        new(userId, productId, request.FdcId);

    public static UnlinkProductFromUsdaFoodCommand ToUnlinkCommand(Guid userId, Guid productId) =>
        new(userId, productId);

    public static GetDailyMicronutrientsQuery ToDailyQuery(Guid userId, DateTime date) =>
        new(userId, date);

    public static UsdaFoodHttpResponse ToHttpResponse(this UsdaFoodModel model) =>
        new(model.FdcId, model.Description, model.FoodCategory);

    public static IReadOnlyList<UsdaFoodHttpResponse> ToHttpResponse(
        this IReadOnlyList<UsdaFoodModel> models) =>
        models.Select(m => m.ToHttpResponse()).ToList();

    public static UsdaFoodDetailHttpResponse ToHttpResponse(this UsdaFoodDetailModel model) =>
        new(model.FdcId,
            model.Description,
            model.FoodCategory,
            model.Nutrients.Select(n => new MicronutrientHttpResponse(
                n.NutrientId, n.Name, n.Unit, n.AmountPer100g,
                n.DailyValue, n.PercentDailyValue)).ToList(),
            model.Portions.Select(p => new UsdaFoodPortionHttpResponse(
                p.Id, p.Amount, p.MeasureUnitName, p.GramWeight,
                p.PortionDescription, p.Modifier)).ToList());

    public static DailyMicronutrientSummaryHttpResponse ToHttpResponse(
        this DailyMicronutrientSummaryModel model) =>
        new(model.Date,
            model.LinkedProductCount,
            model.TotalProductCount,
            model.Nutrients.Select(n => new DailyMicronutrientHttpResponse(
                n.NutrientId, n.Name, n.Unit, n.TotalAmount,
                n.DailyValue, n.PercentDailyValue)).ToList());
}
