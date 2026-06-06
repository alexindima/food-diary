using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAuthenticationTokenService authenticationTokenService) : ICommandHandler<LoginCommand, Result<AuthenticationModel>> {
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IAuthenticationTokenService _authenticationTokenService = authenticationTokenService;

    public async Task<Result<AuthenticationModel>> Handle(LoginCommand command, CancellationToken cancellationToken) {
        User? user = await _userRepository.GetByEmailIncludingDeletedAsync(command.Email, cancellationToken).ConfigureAwait(false);
        if (user == null || !_passwordHasher.Verify(command.Password, user.Password)) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidCredentials);
        }

        Error? accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(accessError);
        }

        IssuedAuthenticationTokens tokens = await _authenticationTokenService
            .IssueAndStoreAsync(user, cancellationToken, command.ClientContext, command.RememberMe)
            .ConfigureAwait(false);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
