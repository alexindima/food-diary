using FoodDiary.Application.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Responses;

namespace FoodDiary.Presentation.Api.Features.Users.Mappings;

public static class UserHttpResponseMappings {
    public static UserHttpResponse ToHttpResponse(this UserModel model) {
        return new UserHttpResponse(
            model.Id,
            model.Email,
            model.Username,
            model.FirstName,
            model.LastName,
            model.BirthDate,
            model.Gender,
            model.Weight,
            model.DesiredWeight,
            model.DesiredWaist,
            model.Height,
            model.ActivityLevel,
            model.DailyCalorieTarget,
            model.ProteinTarget,
            model.FatTarget,
            model.CarbTarget,
            model.FiberTarget,
            model.StepGoal,
            model.WaterGoal,
            model.HydrationGoal,
            model.Language,
            model.ProfileImage,
            model.ProfileImageAssetId,
            model.DashboardLayout?.ToHttpModel(),
            model.IsActive,
            model.IsEmailConfirmed,
            model.LastLoginAtUtc
        );
    }

    public static UserDesiredWeightHttpResponse ToHttpResponse(this UserDesiredWeightModel model)
        => new(model.DesiredWeight);

    public static UserDesiredWaistHttpResponse ToHttpResponse(this UserDesiredWaistModel model)
        => new(model.DesiredWaist);

    private static DashboardLayoutHttpModel ToHttpModel(this DashboardLayoutModel model)
        => new(model.Web, model.Mobile);
}
