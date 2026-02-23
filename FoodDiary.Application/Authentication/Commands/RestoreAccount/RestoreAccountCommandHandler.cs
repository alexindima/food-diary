using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.RestoreAccount;

public class RestoreAccountCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<RestoreAccountCommand, Result<AuthenticationResponse>> {
    public async Task<Result<AuthenticationResponse>> Handle(RestoreAccountCommand command, CancellationToken cancellationToken) {
        var user = await userRepository.GetByEmailIncludingDeletedAsync(command.Email);
        if (user is null || !passwordHasher.Verify(command.Password, user.Password)) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidCredentials);
        }

        if (user.DeletedAt is null) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AccountNotDeleted);
        }

        user.Restore();

        var tokens = await authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);
        var userResponse = user.ToResponse();
        var authResponse = new AuthenticationResponse(tokens.AccessToken, tokens.RefreshToken, userResponse);
        return Result.Success(authResponse);
    }
}
