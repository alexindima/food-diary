using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Presentation.Goals.Responses;

namespace FoodDiary.Modules.Users.Presentation.Goals.Mappings;

public static class GoalsHttpResponseMappings {
    extension(GoalsModel model) {
        public GoalsHttpResponse ToHttpResponse()
                => new(
                    model.DailyCalorieTarget,
                    model.ProteinTarget,
                    model.FatTarget,
                    model.CarbTarget,
                    model.FiberTarget,
                    model.WaterGoal,
                    model.DesiredWeightKg,
                    model.DesiredWaistCm,
                    model.CalorieCyclingEnabled,
                    model.MondayCalories,
                    model.TuesdayCalories,
                    model.WednesdayCalories,
                    model.ThursdayCalories,
                    model.FridayCalories,
                    model.SaturdayCalories,
                    model.SundayCalories
                );
    }
}
