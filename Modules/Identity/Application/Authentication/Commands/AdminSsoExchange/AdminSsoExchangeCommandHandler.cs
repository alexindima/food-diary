using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Identity.Application.Authentication.Services;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.AdminSsoExchange;

public sealed class AdminSsoExchangeCommandHandler(
    IAdminSsoService adminSsoService,
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService,
    IAdminSsoSessionLock sessions,
    IAtomicCommandExecutor atomicExecutor,
    IUnitOfWork unitOfWork,
    PrivilegedSessionService privilegedSessions)
    : ICommandHandler<AdminSsoExchangeCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(
        AdminSsoExchangeCommand command,
        CancellationToken cancellationToken) {
        AdminSsoIssuer? issuer = await adminSsoService.ExchangeCodeAsync(command.Code, cancellationToken);
        if (issuer is null) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.AdminSsoInvalidCode);
        }

        return await atomicExecutor.ExecuteAsync(
            token => ExchangeAsync(issuer, command, token), cancellationToken);
    }

    private async Task<Result<AuthenticationModel>> ExchangeAsync(
        AdminSsoIssuer issuer, AdminSsoExchangeCommand command, CancellationToken cancellationToken) {
        Result<UserAuthenticationPrincipalModel> principalResult = await userIdentityService
            .RecordAuthenticationAsync(
                issuer.UserId,
                dateTimeProvider.GetUtcNow().UtcDateTime,
                cancellationToken);
        if (principalResult.IsFailure) {
            Error error = string.Equals(principalResult.Error.Code, "Authentication.InvalidCredentials", StringComparison.Ordinal)
                ? UserErrors.NotFound()
                : principalResult.Error;
            return Result.Failure<AuthenticationModel>(error);
        }

        UserAuthenticationPrincipalModel principal = principalResult.Value;
        if (principal.SecurityVersion != issuer.SecurityVersion) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.AdminSsoInvalidCode);
        }
        if (!principal.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal)) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.AdminSsoForbidden);
        }

        // Keep the same principal-before-session lock order as password changes.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (!await sessions.LockActiveForAdminSsoAsync(issuer.UserId, new RefreshTokenSessionId(issuer.SessionId), cancellationToken)) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.AdminSsoInvalidCode);
        }

        if (!await privilegedSessions.IsConfirmedAsync(issuer.UserId, issuer.SessionId, allowAdminSession: false, cancellationToken)) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.ReauthenticationRequired);
        }

        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(principal, cancellationToken,
                command.ClientContext is { } client ? client with { AdminAccessConfirmed = true } : null);
        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User, tokens.BrowserSession));
    }
}
