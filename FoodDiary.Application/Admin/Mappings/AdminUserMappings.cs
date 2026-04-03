using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Admin.Mappings;

public static class AdminUserMappings {
    public static AdminUserModel ToAdminModel(this User user) {
        var roles = user.GetRoleNames().ToArray();

        return new AdminUserModel(
            user.Id.Value,
            user.Email,
            user.Username,
            user.FirstName,
            user.LastName,
            user.Language,
            user.IsActive,
            user.IsEmailConfirmed,
            user.CreatedOnUtc,
            user.DeletedAt,
            user.LastLoginAtUtc,
            roles,
            user.AiInputTokenLimit,
            user.AiOutputTokenLimit);
    }
}
