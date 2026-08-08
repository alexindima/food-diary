using FoodDiary.Application.Users.Commands.UpdateGoals;
using FoodDiary.Application.Users.Queries.GetUserGoals;
using FoodDiary.Presentation.Api.Features.Goals.Requests;

namespace FoodDiary.Presentation.Api.Features.Goals.Mappings;

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
                    request.DesiredWeight,
                    request.DesiredWaist,
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
