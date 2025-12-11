using FoodDiary.Contracts.Users;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Users.Mappings;

public static class UserMappings
{
    public static UserResponse ToResponse(this User user)
    {
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
            user.ProfileImage,
            user.IsActive
        );
    }
}
