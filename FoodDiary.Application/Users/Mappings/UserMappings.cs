using FoodDiary.Contracts.Users;
using FoodDiary.Contracts.Goals;
using FoodDiary.Domain.Entities;
using System.Text.Json;

namespace FoodDiary.Application.Users.Mappings;

public static class UserMappings
{
    public static UserResponse ToResponse(this User user)
    {
        const string defaultLanguage = "en";
        DashboardLayoutSettings? layout = null;
        if (!string.IsNullOrWhiteSpace(user.DashboardLayoutJson))
        {
            try
            {
                layout = JsonSerializer.Deserialize<DashboardLayoutSettings>(user.DashboardLayoutJson);
            }
            catch (JsonException)
            {
                layout = null;
            }
        }

        return new UserResponse(
            user.Id.Value,
            user.Email,
            user.Username,
            user.FirstName,
            user.LastName,
            user.BirthDate,
            user.Gender,
            user.Weight,
            user.DesiredWeight,
            user.DesiredWaist,
            user.Height,
            user.ActivityLevel.ToString(),
            user.DailyCalorieTarget,
            user.ProteinTarget,
            user.FatTarget,
            user.CarbTarget,
            user.FiberTarget,
            user.StepGoal,
            user.WaterGoal,
            user.HydrationGoal,
            user.Language ?? defaultLanguage,
            user.ProfileImage,
            user.ProfileImageAssetId?.Value,
            layout,
            user.IsActive,
            user.IsEmailConfirmed,
            user.LastLoginAtUtc
        );
    }

    public static GoalsResponse ToGoalsResponse(this User user)
    {
        return new GoalsResponse(
            user.DailyCalorieTarget,
            user.ProteinTarget,
            user.FatTarget,
            user.CarbTarget,
            user.FiberTarget,
            user.WaterGoal,
            user.DesiredWeight,
            user.DesiredWaist
        );
    }
}
