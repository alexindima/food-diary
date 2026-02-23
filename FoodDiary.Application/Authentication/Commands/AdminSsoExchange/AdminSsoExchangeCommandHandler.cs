using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Contracts.Authentication;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Application.Users.Mappings;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoExchange;

public sealed class AdminSsoExchangeCommandHandler(
    IAdminSsoService adminSsoService,
    IUserRepository userRepository,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<AdminSsoExchangeCommand, Result<AuthenticationResponse>> {
    public async Task<Result<AuthenticationResponse>> Handle(
        AdminSsoExchangeCommand command,
        CancellationToken cancellationToken) {
        var userId = await adminSsoService.ExchangeCodeAsync(command.Code, cancellationToken);
        if (userId is null) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AdminSsoInvalidCode);
        }

        var user = await userRepository.GetByIdAsync(userId.Value);
        if (user is null) {
            return Result.Failure<AuthenticationResponse>(Errors.User.NotFound());
        }

        if (user.DeletedAt is not null) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AccountDeleted);
        }

        if (!IsAdmin(user)) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AdminSsoForbidden);
        }

        var tokens = await authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);

        var userResponse = user.ToResponse();
        var authResponse = new AuthenticationResponse(tokens.AccessToken, tokens.RefreshToken, userResponse);
        return Result.Success(authResponse);
    }

    private static bool IsAdmin(User user) {
        return user.UserRoles.Any(role => string.Equals(role.Role.Name, RoleNames.Admin, StringComparison.Ordinal));
    }
}
