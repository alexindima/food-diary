using FoodDiary.Contracts.Admin;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Admin.Mappings;

public static class AdminUserMappings
{
    public static AdminUserResponse ToAdminResponse(this User user)
    {
        var roles = user.UserRoles
            .Select(role => role.Role.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToArray();

        return new AdminUserResponse(
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


