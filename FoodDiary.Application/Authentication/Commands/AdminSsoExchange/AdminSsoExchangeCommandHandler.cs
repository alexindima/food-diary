using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoExchange;

public sealed class AdminSsoExchangeCommandHandler(
    IAdminSsoService adminSsoService,
    IUserRepository userRepository,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<AdminSsoExchangeCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(
        AdminSsoExchangeCommand command,
        CancellationToken cancellationToken) {
        var userId = await adminSsoService.ExchangeCodeAsync(command.Code, cancellationToken);
        if (userId is null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.AdminSsoInvalidCode);
        }

        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null) {
            return Result.Failure<AuthenticationModel>(Errors.User.NotFound());
        }

        var accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(accessError);
        }
        if (user is null) {
            return Result.Failure<AuthenticationModel>(Errors.User.NotFound());
        }

        if (!IsAdmin(user)) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.AdminSsoForbidden);
        }

        var tokens = await authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }

    private static bool IsAdmin(User user) {
        return user.HasRole(RoleNames.Admin);
    }
}
