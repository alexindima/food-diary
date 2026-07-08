using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoStart;

public sealed class AdminSsoStartCommandHandler(
    IAdminSsoService adminSsoService,
    IAuthenticationUserLookupService userLookupService)
    : ICommandHandler<AdminSsoStartCommand, Result<AdminSsoStartModel>> {
    public async Task<Result<AdminSsoStartModel>> Handle(
        AdminSsoStartCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            command.UserId,
            Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<AdminSsoStartModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        User? user = await userLookupService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
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
