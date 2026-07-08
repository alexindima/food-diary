using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Results;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;

public sealed class BootstrapInitialAdminCommandHandler(
    IAuthenticationUserRegistrationService userRegistrationService,
    IPasswordHasher passwordHasher)
    : ICommandHandler<BootstrapInitialAdminCommand, Result<BootstrapInitialAdminModel>> {
    private static readonly string[] BootstrapRoles = [
        RoleNames.Owner,
        RoleNames.Admin,
        RoleNames.Premium,
    ];

    public async Task<Result<BootstrapInitialAdminModel>> Handle(
        BootstrapInitialAdminCommand command,
        CancellationToken cancellationToken) {
        string normalizedEmail = command.Email.Trim();

        if (string.IsNullOrWhiteSpace(command.Password)) {
            return Result.Success(new BootstrapInitialAdminModel(
                BootstrapInitialAdminStatus.SkippedMissingPassword,
                normalizedEmail));
        }

        User? existingUser = await userRegistrationService.GetByEmailIncludingDeletedAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        if (existingUser is not null) {
            return Result.Success(new BootstrapInitialAdminModel(
                BootstrapInitialAdminStatus.SkippedExistingUser,
                normalizedEmail));
        }

        IReadOnlyList<Role> roles = await userRegistrationService.EnsureRolesByNamesAsync(BootstrapRoles, cancellationToken).ConfigureAwait(false);
        var admin = User.Create(normalizedEmail, passwordHasher.Hash(command.Password));
        admin.SetEmailConfirmed(isConfirmed: true);
        admin.ReplaceRoles(roles);

        await userRegistrationService.AddAsync(admin, cancellationToken).ConfigureAwait(false);

        return Result.Success(new BootstrapInitialAdminModel(
            BootstrapInitialAdminStatus.Created,
            normalizedEmail));
    }
}
