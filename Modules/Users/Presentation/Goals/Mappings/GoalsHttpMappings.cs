using FoodDiary.Modules.Users.Application.Commands.UpdateGoals;
using FoodDiary.Modules.Users.Application.Queries.GetUserGoals;
using FoodDiary.Modules.Users.Presentation.Goals.Requests;

namespace FoodDiary.Modules.Users.Presentation.Goals.Mappings;

public static class GoalsHttpMappings {
    extension(Guid userId) {
        public GetUserGoalsQuery ToQuery() => new(userId);
    }

    extension(UpdateGoalsHttpRequest request) {
        public UpdateGoalsCommand ToCommand(Guid? userId)
                => new(
                    userId,
                    request.DailyCalorieTarget,
                    request.ProteinTarget,
                    request.FatTarget,
                    request.CarbTarget,
                    request.FiberTarget,
                    request.WaterGoal,
                    request.DesiredWeightKg,
                    request.DesiredWaistCm,
                    request.CalorieCyclingEnabled,
                    request.MondayCalories,
                    request.TuesdayCalories,
                    request.WednesdayCalories,
                    request.ThursdayCalories,
                    request.FridayCalories,
                    request.SaturdayCalories,
                    request.SundayCalories
                );
    }
}
