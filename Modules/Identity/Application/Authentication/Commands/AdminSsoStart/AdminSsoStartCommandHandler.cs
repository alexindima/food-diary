using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.AdminSsoStart;

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
                ? AuthenticationErrors.InvalidCredentials
                : principalResult.Error;
            return Result.Failure<AdminSsoStartModel>(error);
        }

        if (!principalResult.Value.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal)) {
            return Result.Failure<AdminSsoStartModel>(IdentityErrors.AdminSsoForbidden);
        }

        AdminSsoCode code = await adminSsoService.CreateCodeAsync(userId, cancellationToken).ConfigureAwait(false);
        var response = new AdminSsoStartModel(code.Code, code.ExpiresAtUtc);
        return Result.Success(response);
    }
}
