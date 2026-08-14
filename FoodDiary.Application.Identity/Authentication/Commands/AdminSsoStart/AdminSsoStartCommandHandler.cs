using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Identity.Authentication.Commands.AdminSsoStart;

public sealed class AdminSsoStartCommandHandler(
    IAdminSsoService adminSsoService,
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider)
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
        Result<UserAuthenticationPrincipalModel> principalResult = await userIdentityService
            .GetAuthenticationPrincipalAsync(
                userId,
                dateTimeProvider.GetUtcNow().UtcDateTime,
                cancellationToken)
            .ConfigureAwait(false);
        if (principalResult.IsFailure) {
            Error error = string.Equals(principalResult.Error.Code, "User.NotFound", StringComparison.Ordinal)
                ? Errors.Authentication.InvalidCredentials
                : principalResult.Error;
            return Result.Failure<AdminSsoStartModel>(error);
        }

        if (!principalResult.Value.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal)) {
            return Result.Failure<AdminSsoStartModel>(Errors.Authentication.AdminSsoForbidden);
        }

        AdminSsoCode code = await adminSsoService.CreateCodeAsync(userId, cancellationToken).ConfigureAwait(false);
        var response = new AdminSsoStartModel(code.Code, code.ExpiresAtUtc);
        return Result.Success(response);
    }
}
