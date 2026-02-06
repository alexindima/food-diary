using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Admin;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminUser;

public sealed class UpdateAdminUserCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateAdminUserCommand, Result<AdminUserResponse>>
{
    public async Task<Result<AdminUserResponse>> Handle(
        UpdateAdminUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdIncludingDeletedAsync(command.UserId);
        if (user is null)
        {
            return Result.Failure<AdminUserResponse>(Errors.User.NotFound(command.UserId.Value));
        }

        if (command.IsActive.HasValue)
        {
            if (command.IsActive.Value)
            {
                user.Activate();
            }
            else
            {
                user.Deactivate();
            }
        }

        if (command.Roles is not null)
        {
            var requestedRoles = command.Roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var allowedRoles = new HashSet<string>(
                new[] { RoleNames.Admin, RoleNames.Premium, RoleNames.Support },
                StringComparer.Ordinal);

            if (requestedRoles.Any(role => !allowedRoles.Contains(role)))
            {
                return Result.Failure<AdminUserResponse>(
                    Errors.Validation.Invalid("roles", "Unknown role."));
            }

            var roleEntities = await userRepository.GetRolesByNamesAsync(requestedRoles);
            UpdateUserRoles(user, roleEntities);
        }

        if (command.AiInputTokenLimit.HasValue || command.AiOutputTokenLimit.HasValue)
        {
            if (command.AiInputTokenLimit.HasValue && command.AiInputTokenLimit.Value < 0)
            {
                return Result.Failure<AdminUserResponse>(
                    Errors.Validation.Invalid("aiInputTokenLimit", "AI input token limit must be non-negative."));
            }

            if (command.AiOutputTokenLimit.HasValue && command.AiOutputTokenLimit.Value < 0)
            {
                return Result.Failure<AdminUserResponse>(
                    Errors.Validation.Invalid("aiOutputTokenLimit", "AI output token limit must be non-negative."));
            }

            user.UpdateAiTokenLimits(command.AiInputTokenLimit, command.AiOutputTokenLimit);
        }

        await userRepository.UpdateAsync(user);
        return Result.Success(user.ToAdminResponse());
    }

    private static void UpdateUserRoles(User user, IReadOnlyList<Role> roles)
    {
        user.UserRoles.Clear();
        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole(user.Id, role.Id));
        }
    }
}
