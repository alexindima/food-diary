using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.RestoreAccount;

public class RestoreAccountCommandHandler(
    IAuthenticationUserMutationService userMutationService,
    IPasswordHasher passwordHasher,
    IAuthenticationTokenService authenticationTokenService,
    TimeProvider dateTimeProvider)
    : ICommandHandler<RestoreAccountCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(RestoreAccountCommand command, CancellationToken cancellationToken) {
        User? user = await userMutationService.GetByEmailIncludingDeletedAsync(command.Email, cancellationToken).ConfigureAwait(false);
        if (user is null || !passwordHasher.Verify(command.Password, user.Password)) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidCredentials);
        }

        if (user.DeletedAt is null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.AccountNotDeleted);
        }

        user.Restore(dateTimeProvider.GetUtcNow().UtcDateTime);

        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueAndStoreAsync(user, cancellationToken, command.ClientContext, command.RememberMe)
            .ConfigureAwait(false);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
