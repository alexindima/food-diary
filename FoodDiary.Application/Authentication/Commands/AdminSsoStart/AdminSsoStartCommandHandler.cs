using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoStart;

public sealed class AdminSsoStartCommandHandler(
    IAdminSsoService adminSsoService,
    IUserRepository userRepository)
    : ICommandHandler<AdminSsoStartCommand, Result<AdminSsoStartModel>> {
    public async Task<Result<AdminSsoStartModel>> Handle(
        AdminSsoStartCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId == Guid.Empty) {
            return Result.Failure<AdminSsoStartModel>(
                Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        }

        var userId = new UserId(command.UserId);
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AdminSsoStartModel>(accessError);
        }

        if (!user!.HasRole(RoleNames.Admin)) {
            return Result.Failure<AdminSsoStartModel>(Errors.Authentication.AdminSsoForbidden);
        }

        AdminSsoCode code = await adminSsoService.CreateCodeAsync(userId, cancellationToken).ConfigureAwait(false);
        var response = new AdminSsoStartModel(code.Code, code.ExpiresAtUtc);
        return Result.Success(response);
    }
}
