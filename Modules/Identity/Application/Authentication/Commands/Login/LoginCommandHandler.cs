using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.Login;

public sealed class LoginCommandHandler(
    PasswordAuthenticationService passwords,
    IAuthenticationTokenService authenticationTokenService) : ICommandHandler<LoginCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(LoginCommand command, CancellationToken cancellationToken) {
        Result<UserAuthenticationPrincipalModel> authenticationResult = await passwords.AuthenticateAsync(command.Email, command.Password, cancellationToken);
        if (authenticationResult.IsFailure) {
            return Result.Failure<AuthenticationModel>(authenticationResult.Error);
        }

        UserAuthenticationPrincipalModel principal = authenticationResult.Value;
        if (command.ExpectedUserId is { } expectedUserId && principal.UserId.Value != expectedUserId) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.ReauthenticationAccountMismatch);
        }
        if (command.ClientContext?.CredentialKind == AuthenticationCredentialKind.AdminBrowser &&
            !principal.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal)) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.AdminSsoForbidden);
        }
        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(principal, cancellationToken, command.ClientContext, command.RememberMe);
        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User, tokens.BrowserSession));
    }
}
