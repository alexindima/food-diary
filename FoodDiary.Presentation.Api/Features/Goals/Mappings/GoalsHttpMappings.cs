using FoodDiary.Application.Users.Commands.UpdateGoals;
using FoodDiary.Application.Users.Queries.GetUserGoals;
using FoodDiary.Presentation.Api.Features.Goals.Requests;

namespace FoodDiary.Presentation.Api.Features.Goals.Mappings;

public static class GoalsHttpMappings {
    public static GetUserGoalsQuery ToQuery(this Guid userId) => new(userId);

    public static UpdateGoalsCommand ToCommand(this UpdateGoalsHttpRequest request, Guid? userId)
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
