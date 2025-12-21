using FoodDiary.Contracts.Goals;
using FoodDiary.Application.Users.Commands.UpdateGoals;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Mappings;

public static class GoalsCommandMappings
{
    public static UpdateGoalsCommand ToCommand(this UpdateGoalsRequest request, UserId? userId)
        => new(
            userId,
            request.DailyCalorieTarget,
            request.ProteinTarget,
            request.FatTarget,
            request.CarbTarget,
            request.FiberTarget,
            request.WaterGoal,
            request.DesiredWeight,
            request.DesiredWaist
        );
}
