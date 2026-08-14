using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Presentation.Api.Features.Meals.Mappings;
using FoodDiary.Presentation.Api.Features.Cycles.Mappings;
using FoodDiary.Presentation.Api.Features.Dashboard.Responses;
using FoodDiary.Presentation.Api.Features.Fasting.Mappings;
using FoodDiary.Presentation.Api.Features.Hydration.Mappings;
using FoodDiary.Presentation.Api.Features.Tdee.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;
using FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Dashboard.Mappings;

public static class DashboardHttpResponseMappings {
    extension(DashboardSnapshotModel model) {
        public DashboardSnapshotHttpResponse ToHttpResponse() {
            return new DashboardSnapshotHttpResponse(
                model.Date,
                model.DateTo,
                model.DailyGoal,
                model.WeeklyCalorieGoal,
                model.Statistics.ToHttpResponse(),
                model.WeeklyCalories.ToHttpResponseList(ToHttpResponse),
                model.Weight.ToHttpResponse(),
                model.Waist.ToHttpResponse(),
                model.Meals.ToHttpResponse(),
                model.Hydration?.ToHttpResponse(),
                model.Advice?.ToHttpResponse(),
                model.CurrentFastingSession?.ToHttpResponse(),
                model.WeightTrend?.ToHttpResponseList(static item => item.ToHttpResponse()),
                model.WaistTrend?.ToHttpResponseList(static item => item.ToHttpResponse()),
                model.DashboardLayout is null
                    ? null
                    : new DashboardLayoutHttpModel(model.DashboardLayout.Web, model.DashboardLayout.Mobile),
                model.CaloriesBurned,
                model.TdeeInsight?.ToHttpResponse(),
                model.CurrentCycle?.ToHttpResponse()
            );
        }
    }

    extension(DashboardStatisticsModel model) {
        private DashboardStatisticsHttpResponse ToHttpResponse() {
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
    }

    extension(DailyCaloriesModel model) {
        private DailyCaloriesHttpResponse ToHttpResponse() {
            return new DailyCaloriesHttpResponse(model.Date, model.Calories, model.Proteins, model.Fats, model.Carbs, model.Fiber);
        }
    }

    extension(DashboardWeightModel model) {
        private DashboardWeightHttpResponse ToHttpResponse() {
            return new DashboardWeightHttpResponse(
                model.Latest is null ? null : new WeightPointHttpResponse(model.Latest.Date, model.Latest.WeightKg),
                model.Previous is null ? null : new WeightPointHttpResponse(model.Previous.Date, model.Previous.WeightKg),
                model.DesiredWeightKg
            );
        }
    }

    extension(DashboardWaistModel model) {
        private DashboardWaistHttpResponse ToHttpResponse() {
            return new DashboardWaistHttpResponse(
                model.Latest is null ? null : new WaistPointHttpResponse(model.Latest.Date, model.Latest.CircumferenceCm),
                model.Previous is null ? null : new WaistPointHttpResponse(model.Previous.Date, model.Previous.CircumferenceCm),
                model.DesiredWaistCm
            );
        }
    }

    extension(DashboardMealsModel model) {
        private DashboardMealsHttpResponse ToHttpResponse() {
            return new DashboardMealsHttpResponse(
                model.Items.ToHttpResponseList(static item => item.ToHttpResponse()),
                model.Total
            );
        }
    }

    extension(DailyAdviceModel model) {
        public DailyAdviceHttpResponse ToHttpResponse() {
            return new DailyAdviceHttpResponse(
                model.Id,
                model.Locale,
                model.Value,
                model.Tag,
                model.Weight
            );
        }
    }
}
