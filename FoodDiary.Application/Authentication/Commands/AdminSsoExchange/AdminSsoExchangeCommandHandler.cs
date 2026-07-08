using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoExchange;

public sealed class AdminSsoExchangeCommandHandler(
    IAdminSsoService adminSsoService,
    IAuthenticationUserLookupService userLookupService,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<AdminSsoExchangeCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(
        AdminSsoExchangeCommand command,
        CancellationToken cancellationToken) {
        UserId? userId = await adminSsoService.ExchangeCodeAsync(command.Code, cancellationToken).ConfigureAwait(false);
        if (userId is null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.AdminSsoInvalidCode);
        }

        User? user = await userLookupService.GetByIdAsync(userId.Value, cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<AuthenticationModel>(Errors.User.NotFound());
        }

        Error? accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(accessError);
        }

        if (!IsAdmin(user)) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.AdminSsoForbidden);
        }

        IssuedAuthenticationTokens tokens = await authenticationTokenService.IssueAndStoreAsync(user, cancellationToken, command.ClientContext).ConfigureAwait(false);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }

    private static bool IsAdmin(User user) {
        return user.HasRole(RoleNames.Admin);
    }
}
