using FoodDiary.Application.Users.Commands.UpdateGoals;
using FoodDiary.Application.Users.Queries.GetUserGoals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Goals.Requests;

namespace FoodDiary.Presentation.Api.Features.Goals.Mappings;

public static class GoalsHttpMappings {
    public static GetUserGoalsQuery ToQuery(this UserId userId) => new(userId);

    public static UpdateGoalsCommand ToCommand(this UpdateGoalsHttpRequest request, UserId? userId)
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
