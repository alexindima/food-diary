using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Presentation.Api.Features.Consumptions.Mappings;
using FoodDiary.Presentation.Api.Features.Dashboard.Responses;
using FoodDiary.Presentation.Api.Features.Hydration.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;
using FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Dashboard.Mappings;

public static class DashboardHttpResponseMappings {
    public static DashboardSnapshotHttpResponse ToHttpResponse(this DashboardSnapshotModel model) {
        return new DashboardSnapshotHttpResponse(
            model.Date,
            model.DailyGoal,
            model.WeeklyCalorieGoal,
            model.Statistics.ToHttpResponse(),
            model.WeeklyCalories.ToHttpResponseList(ToHttpResponse),
            model.Weight.ToHttpResponse(),
            model.Waist.ToHttpResponse(),
            model.Meals.ToHttpResponse(),
            model.Hydration?.ToHttpResponse(),
            model.Advice?.ToHttpResponse(),
            model.WeightTrend?.ToHttpResponseList(static item => item.ToHttpResponse()),
            model.WaistTrend?.ToHttpResponseList(static item => item.ToHttpResponse()),
            model.DashboardLayout is null
                ? null
                : new DashboardLayoutHttpModel(model.DashboardLayout.Web, model.DashboardLayout.Mobile)
        );
    }

    private static DashboardStatisticsHttpResponse ToHttpResponse(this DashboardStatisticsModel model) {
        return new DashboardStatisticsHttpResponse(
            model.TotalCalories,
            model.AverageProteins,
            model.AverageFats,
            model.AverageCarbs,
            model.AverageFiber,
            model.ProteinGoal,
            model.FatGoal,
            model.CarbGoal,
            model.FiberGoal
        );
    }

    private static DailyCaloriesHttpResponse ToHttpResponse(this DailyCaloriesModel model) {
        return new DailyCaloriesHttpResponse(model.Date, model.Calories);
    }

    private static DashboardWeightHttpResponse ToHttpResponse(this DashboardWeightModel model) {
        return new DashboardWeightHttpResponse(
            model.Latest is null ? null : new WeightPointHttpResponse(model.Latest.Date, model.Latest.Weight),
            model.Previous is null ? null : new WeightPointHttpResponse(model.Previous.Date, model.Previous.Weight),
            model.Desired
        );
    }

    private static DashboardWaistHttpResponse ToHttpResponse(this DashboardWaistModel model) {
        return new DashboardWaistHttpResponse(
            model.Latest is null ? null : new WaistPointHttpResponse(model.Latest.Date, model.Latest.Circumference),
            model.Previous is null ? null : new WaistPointHttpResponse(model.Previous.Date, model.Previous.Circumference),
            model.Desired
        );
    }

    private static DashboardMealsHttpResponse ToHttpResponse(this DashboardMealsModel model) {
        return new DashboardMealsHttpResponse(
            model.Items.ToHttpResponseList(static item => item.ToHttpResponse()),
            model.Total
        );
    }

    public static DailyAdviceHttpResponse ToHttpResponse(this DailyAdviceModel model) {
        return new DailyAdviceHttpResponse(
            model.Id,
            model.Locale,
            model.Value,
            model.Tag,
            model.Weight
        );
    }
}
