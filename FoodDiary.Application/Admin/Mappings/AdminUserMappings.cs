using FoodDiary.Contracts.Admin;
using FoodDiary.Domain.Entities;

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
