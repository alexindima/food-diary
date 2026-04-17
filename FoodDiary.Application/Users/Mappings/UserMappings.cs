using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Entities.Users;
using System.Text.Json;

namespace FoodDiary.Application.Users.Mappings;

public static class UserMappings {
    public static UserModel ToModel(this User user) {
        const string defaultLanguage = "en";
        const string defaultTheme = "ocean";
        DashboardLayoutModel? layout = null;
        if (!string.IsNullOrWhiteSpace(user.DashboardLayoutJson)) {
            try {
                layout = JsonSerializer.Deserialize<DashboardLayoutModel>(user.DashboardLayoutJson);
            } catch (JsonException) {
                layout = null;
            }
        }

        return new UserModel(
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
            user.Theme ?? defaultTheme,
            user.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours,
            user.ProfileImage,
            user.ProfileImageAssetId?.Value,
            layout,
            user.IsActive,
            user.IsEmailConfirmed,
            user.LastLoginAtUtc,
            user.AiConsentAcceptedAt
        );
    }

    public static GoalsModel ToGoalsModel(this User user) {
        return new GoalsModel(
            user.DailyCalorieTarget,
            user.ProteinTarget,
            user.FatTarget,
            user.CarbTarget,
            user.FiberTarget,
            user.WaterGoal,
            user.DesiredWeight,
            user.DesiredWaist,
            user.CalorieCyclingEnabled,
            user.MondayCalories,
            user.TuesdayCalories,
            user.WednesdayCalories,
            user.ThursdayCalories,
            user.FridayCalories,
            user.SaturdayCalories,
            user.SundayCalories
        );
    }
}
