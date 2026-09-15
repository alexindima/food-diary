using FoodDiary.Modules.Usda.Contracts.Queries.SearchUsdaFoods;
using FoodDiary.Modules.Usda.Application.Commands.LinkProductToUsdaFood;
using FoodDiary.Modules.Usda.Application.Commands.UnlinkProductFromUsdaFood;
using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Usda.Application.Queries.GetDailyMicronutrients;
using FoodDiary.Modules.Usda.Application.Queries.GetMicronutrients;
using FoodDiary.Modules.Usda.Presentation.Requests;
using FoodDiary.Modules.Usda.Presentation.Responses;

namespace FoodDiary.Modules.Usda.Presentation.Mappings;

public static class UsdaHttpMappings {
    public static SearchUsdaFoodsQuery ToQuery(string search, int limit) =>
        new(search, limit);

    public static GetMicronutrientsQuery ToQuery(int fdcId) =>
        new(fdcId);

    extension(LinkProductToUsdaFoodHttpRequest request) {
        public LinkProductToUsdaFoodCommand ToCommand(
        Guid userId, Guid productId) =>
                new(userId, productId, request.FdcId);
    }

    public static UnlinkProductFromUsdaFoodCommand ToUnlinkCommand(Guid userId, Guid productId) =>
        new(userId, productId);

    public static GetDailyMicronutrientsQuery ToDailyQuery(Guid userId, DateTime date) =>
        new(userId, date);

    extension(UsdaFoodModel model) {
        public UsdaFoodHttpResponse ToHttpResponse() =>
                new(model.FdcId, model.Description, model.FoodCategory);
    }

    extension(IReadOnlyList<UsdaFoodModel> models) {
        public IReadOnlyList<UsdaFoodHttpResponse> ToHttpResponse(
        ) =>
                models.Select(m => m.ToHttpResponse()).ToList();
    }

    extension(UsdaFoodDetailModel model) {
        public UsdaFoodDetailHttpResponse ToHttpResponse() =>
                new(model.FdcId,
                    model.Description,
                    model.FoodCategory,
                    model.Nutrients.Select(n => new MicronutrientHttpResponse(
                        n.NutrientId, n.Name, n.Unit, n.AmountPer100G,
                        n.DailyValue, n.PercentDailyValue)).ToList(),
                    model.Portions.Select(p => new UsdaFoodPortionHttpResponse(
                        p.Id, p.Amount, p.MeasureUnitName, p.GramWeight,
                        p.PortionDescription, p.Modifier)).ToList(),
                    model.HealthScores?.ToHttpResponse());
    }

    extension(DailyMicronutrientSummaryModel model) {
        public DailyMicronutrientSummaryHttpResponse ToHttpResponse(
        ) =>
                new(model.Date,
                    model.LinkedProductCount,
                    model.TotalProductCount,
                    model.Nutrients.Select(n => new DailyMicronutrientHttpResponse(
                        n.NutrientId, n.Name, n.Unit, n.TotalAmount,
                        n.DailyValue, n.PercentDailyValue)).ToList(),
                    model.HealthScores?.ToHttpResponse());
    }

    extension(HealthAreaScoresModel scores) {
        private HealthAreaScoresHttpResponse ToHttpResponse() =>
                new(new HealthAreaScoreHttpResponse(scores.Heart.Score, scores.Heart.Grade),
                    new HealthAreaScoreHttpResponse(scores.Bone.Score, scores.Bone.Grade),
                    new HealthAreaScoreHttpResponse(scores.Immune.Score, scores.Immune.Grade),
                    new HealthAreaScoreHttpResponse(scores.Energy.Score, scores.Energy.Grade),
                    new HealthAreaScoreHttpResponse(scores.Antioxidant.Score, scores.Antioxidant.Grade));
    }
}
