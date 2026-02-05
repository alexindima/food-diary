using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Contracts.Authentication;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Application.Users.Mappings;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoExchange;

public sealed class AdminSsoExchangeCommandHandler(
    IAdminSsoService adminSsoService,
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher passwordHasher)
    : ICommandHandler<AdminSsoExchangeCommand, Result<AuthenticationResponse>>
{
    public async Task<Result<AuthenticationResponse>> Handle(
        AdminSsoExchangeCommand command,
        CancellationToken cancellationToken)
    {
        var userId = await adminSsoService.ExchangeCodeAsync(command.Code, cancellationToken);
        if (userId is null)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AdminSsoInvalidCode);
        }

        var user = await userRepository.GetByIdAsync(userId.Value);
        if (user is null)
        {
            return Result.Failure<AuthenticationResponse>(Errors.User.NotFound());
        }

        if (user.DeletedAt is not null)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AccountDeleted);
        }

        if (!IsAdmin(user))
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AdminSsoForbidden);
        }

        var roles = user.UserRoles.Select(role => role.Role.Name).Where(name => name is not null).ToArray();
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email, roles);
        var hashedRefreshToken = passwordHasher.Hash(refreshToken);
        user.UpdateRefreshToken(hashedRefreshToken);
        await userRepository.UpdateAsync(user);

        var userResponse = user.ToResponse();
        var authResponse = new AuthenticationResponse(accessToken, refreshToken, userResponse);
        return Result.Success(authResponse);
    }

    private static bool IsAdmin(Domain.Entities.User user)
    {
        return user.UserRoles.Any(role => string.Equals(role.Role.Name, RoleNames.Admin, StringComparison.Ordinal));
    }
}
