using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;

namespace FoodDiary.Application.Authentication.Commands.Login;

public class LoginCommandHandler : ICommandHandler<LoginCommand, Result<AuthenticationModel>> {
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthenticationTokenService _authenticationTokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthenticationTokenService authenticationTokenService) {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authenticationTokenService = authenticationTokenService;
    }

    public async Task<Result<AuthenticationModel>> Handle(LoginCommand command, CancellationToken cancellationToken) {
        var user = await _userRepository.GetByEmailIncludingDeletedAsync(command.Email, cancellationToken);
        if (user == null || !_passwordHasher.Verify(command.Password, user.Password)) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidCredentials);
        }

        if (user.DeletedAt is not null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.AccountDeleted);
        }

        if (!user.IsActive) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidCredentials);
        }

        var tokens = await _authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
