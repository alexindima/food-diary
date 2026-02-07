using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using System;

namespace FoodDiary.Application.Authentication.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<VerifyEmailCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId);
        if (user is null)
        {
            return Result.Failure<bool>(Errors.User.NotFound(command.UserId.Value));
        }

        if (user.IsEmailConfirmed)
        {
            return Result.Success(true);
        }

        if (string.IsNullOrWhiteSpace(user.EmailConfirmationTokenHash) ||
            !user.EmailConfirmationTokenExpiresAtUtc.HasValue ||
            user.EmailConfirmationTokenExpiresAtUtc.Value < DateTime.UtcNow)
        {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        var isValid = passwordHasher.Verify(command.Token, user.EmailConfirmationTokenHash);
        if (!isValid)
        {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        user.ConfirmEmail();
        await userRepository.UpdateAsync(user);

        return Result.Success(true);
    }
}
