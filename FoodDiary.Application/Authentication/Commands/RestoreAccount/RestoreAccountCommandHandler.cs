using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.RestoreAccount;

public class RestoreAccountCommandHandler(
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher passwordHasher)
    : ICommandHandler<RestoreAccountCommand, Result<AuthenticationResponse>>
{
    public async Task<Result<AuthenticationResponse>> Handle(RestoreAccountCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailIncludingDeletedAsync(command.Email);
        if (user is null || !passwordHasher.Verify(command.Password, user.Password))
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidCredentials);
        }

        if (user.DeletedAt is null)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AccountNotDeleted);
        }

        user.Restore();

        var refreshToken = jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email);
        var hashedRefreshToken = passwordHasher.Hash(refreshToken);
        user.UpdateRefreshToken(hashedRefreshToken);
        await userRepository.UpdateAsync(user);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);
        var userResponse = user.ToResponse();
        var authResponse = new AuthenticationResponse(accessToken, refreshToken, userResponse);
        return Result.Success(authResponse);
    }
}
