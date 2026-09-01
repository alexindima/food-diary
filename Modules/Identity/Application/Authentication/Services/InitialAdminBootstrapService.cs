using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Identity.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Services;

internal sealed class InitialAdminBootstrapService(
    IUserAuthenticationRegistrationService userRegistrationService,
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

        UserInitialAdminBootstrapModel bootstrap = await userRegistrationService
            .BootstrapInitialAdminAsync(normalizedEmail, password, BootstrapRoles, cancellationToken)
            .ConfigureAwait(false);
        if (!bootstrap.Created) {
            return Result.Success(new BootstrapInitialAdminModel(
                BootstrapInitialAdminStatus.SkippedExistingUser,
                normalizedEmail));
        }
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new BootstrapInitialAdminModel(
            BootstrapInitialAdminStatus.Created,
            normalizedEmail));
    }
}
