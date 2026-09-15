using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.AdminSsoExchange;

public sealed class AdminSsoExchangeCommandHandler(
    IAdminSsoService adminSsoService,
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<AdminSsoExchangeCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(
        AdminSsoExchangeCommand command,
        CancellationToken cancellationToken) {
        UserId? userId = await adminSsoService.ExchangeCodeAsync(command.Code, cancellationToken).ConfigureAwait(false);
        if (userId is null) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.AdminSsoInvalidCode);
        }

        Result<UserAuthenticationPrincipalModel> principalResult = await userIdentityService
            .RecordAuthenticationAsync(
                userId.Value,
                dateTimeProvider.GetUtcNow().UtcDateTime,
                cancellationToken)
            .ConfigureAwait(false);
        if (principalResult.IsFailure) {
            Error error = string.Equals(principalResult.Error.Code, "Authentication.InvalidCredentials", StringComparison.Ordinal)
                ? UserErrors.NotFound()
                : principalResult.Error;
            return Result.Failure<AuthenticationModel>(error);
        }

        UserAuthenticationPrincipalModel principal = principalResult.Value;
        if (!principal.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal)) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.AdminSsoForbidden);
        }

        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(principal, cancellationToken, command.ClientContext)
            .ConfigureAwait(false);
        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User));
    }
}
