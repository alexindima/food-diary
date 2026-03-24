using FoodDiary.Application.Users.Models;
using FoodDiary.Presentation.Api.Features.Goals.Responses;

namespace FoodDiary.Presentation.Api.Features.Goals.Mappings;

public static class GoalsHttpResponseMappings {
    public static GoalsHttpResponse ToHttpResponse(this GoalsModel model)
        => new(
            model.DailyCalorieTarget,
            model.ProteinTarget,
            model.FatTarget,
            model.CarbTarget,
            model.FiberTarget,
            model.WaterGoal,
            model.DesiredWeight,
            model.DesiredWaist
        );
}
