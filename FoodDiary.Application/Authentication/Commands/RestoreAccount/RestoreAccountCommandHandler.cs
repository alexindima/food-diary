using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Authentication.Commands.RestoreAccount;

public class RestoreAccountCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAuthenticationTokenService authenticationTokenService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RestoreAccountCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(RestoreAccountCommand command, CancellationToken cancellationToken) {
        var user = await userRepository.GetByEmailIncludingDeletedAsync(command.Email, cancellationToken);
        if (user is null || !passwordHasher.Verify(command.Password, user.Password)) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidCredentials);
        }

        if (user.DeletedAt is null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.AccountNotDeleted);
        }

        user.Restore(dateTimeProvider.UtcNow);

        var tokens = await authenticationTokenService.IssueAndStoreAsync(user, cancellationToken, command.ClientContext);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
