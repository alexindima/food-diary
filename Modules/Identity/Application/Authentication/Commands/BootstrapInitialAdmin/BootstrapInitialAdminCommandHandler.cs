using FoodDiary.Mediator;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Identity.Contracts.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.BootstrapInitialAdmin;

public sealed class BootstrapInitialAdminCommandHandler(
    IUserAuthenticationRegistrationService userRegistrationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BootstrapInitialAdminCommand, Result<BootstrapInitialAdminModel>> {
    private static readonly string[] BootstrapRoles = [
        RoleNames.Owner,
        RoleNames.Admin,
        RoleNames.Premium,
    ];

    public async Task<Result<BootstrapInitialAdminModel>> Handle(BootstrapInitialAdminCommand request, CancellationToken cancellationToken) {
        string email = request.Email;
        string password = request.Password;
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
