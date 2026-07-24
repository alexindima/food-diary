using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Application.Authentication.Services;

internal sealed class InitialAdminBootstrapService(
    IAuthenticationUserRegistrationService userRegistrationService,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IInitialAdminBootstrapService {
    private static readonly string[] BootstrapRoles = [
        RoleNames.Owner,
        RoleNames.Admin,
        RoleNames.Premium,
    ];

    public async Task<Result<BootstrapInitialAdminModel>> BootstrapAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default) {
        string normalizedEmail = email.Trim();

        if (string.IsNullOrWhiteSpace(password)) {
            return Result.Success(new BootstrapInitialAdminModel(
                BootstrapInitialAdminStatus.SkippedMissingPassword,
                normalizedEmail));
        }

        User? existingUser = await userRegistrationService
            .GetByEmailIncludingDeletedAsync(normalizedEmail, cancellationToken)
            .ConfigureAwait(false);
        if (existingUser is not null) {
            return Result.Success(new BootstrapInitialAdminModel(
                BootstrapInitialAdminStatus.SkippedExistingUser,
                normalizedEmail));
        }

        IReadOnlyList<Role> roles = await userRegistrationService
            .EnsureRolesByNamesAsync(BootstrapRoles, cancellationToken)
            .ConfigureAwait(false);
        var admin = User.Create(normalizedEmail, passwordHasher.Hash(password));
        admin.SetEmailConfirmed(isConfirmed: true);
        admin.ReplaceRoles(roles);

        await userRegistrationService.AddAsync(admin, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new BootstrapInitialAdminModel(
            BootstrapInitialAdminStatus.Created,
            normalizedEmail));
    }
}
