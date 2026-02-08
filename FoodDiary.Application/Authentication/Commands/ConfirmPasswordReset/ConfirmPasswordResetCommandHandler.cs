using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;
using System;
using System.Linq;

namespace FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandHandler(
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher passwordHasher)
    : ICommandHandler<ConfirmPasswordResetCommand, Result<AuthenticationResponse>>
{
    public async Task<Result<AuthenticationResponse>> Handle(ConfirmPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId);
        if (user is null)
        {
            return Result.Failure<AuthenticationResponse>(Errors.User.NotFound(command.UserId.Value));
        }

        if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash) ||
            !user.PasswordResetTokenExpiresAtUtc.HasValue ||
            user.PasswordResetTokenExpiresAtUtc.Value < DateTime.UtcNow)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidToken);
        }

        var isValid = passwordHasher.Verify(command.Token, user.PasswordResetTokenHash);
        if (!isValid)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidToken);
        }

        var hashedPassword = passwordHasher.Hash(command.NewPassword);
        user.UpdatePassword(hashedPassword);
        user.ClearPasswordResetToken();

        var roles = user.UserRoles
            .Select(role => role.Role?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToArray();

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email, roles);

        var hashedRefreshToken = passwordHasher.Hash(refreshToken);
        user.UpdateRefreshToken(hashedRefreshToken);
        await userRepository.UpdateAsync(user);

        var userResponse = user.ToResponse();
        return Result.Success(new AuthenticationResponse(accessToken, refreshToken, userResponse));
    }
}
