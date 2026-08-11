using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Authentication.Commands.LinkGoogle;

public sealed class LinkGoogleCommandHandler(
    IUserContextService userContextService,
    IAuthenticationUserMutationService userMutationService,
    IGoogleTokenValidator googleTokenValidator) : ICommandHandler<LinkGoogleCommand, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(LinkGoogleCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            command.UserId,
            Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<UserModel>(userIdResult);
        }

        Result<GoogleIdentityPayload> payloadResult = await googleTokenValidator
            .ValidateCredentialAsync(command.Credential, cancellationToken)
            .ConfigureAwait(false);
        if (payloadResult.IsFailure) {
            return Result.Failure<UserModel>(payloadResult.Error);
        }

        Result<User> currentUserResult = await userContextService
            .GetAccessibleUserAsync(userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        if (currentUserResult.IsFailure) {
            return Result.Failure<UserModel>(currentUserResult.Error);
        }

        User currentUser = currentUserResult.Value;
        GoogleIdentityPayload payload = payloadResult.Value;
        if (!string.Equals(currentUser.Email, payload.Email, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<UserModel>(Errors.Authentication.GoogleAccountEmailMismatch);
        }

        bool hasCurrentGoogleIdentity =
            !string.IsNullOrWhiteSpace(currentUser.GoogleIssuer) &&
            !string.IsNullOrWhiteSpace(currentUser.GoogleSubject);
        if (hasCurrentGoogleIdentity) {
            bool isSameIdentity =
                string.Equals(currentUser.GoogleIssuer, payload.Issuer, StringComparison.Ordinal) &&
                string.Equals(currentUser.GoogleSubject, payload.Subject, StringComparison.Ordinal);
            return isSameIdentity
                ? Result.Success(currentUser.ToModel())
                : Result.Failure<UserModel>(Errors.Authentication.GoogleIdentityDifferent);
        }

        User? identityOwner = await userMutationService
            .GetByGoogleIdentityIncludingDeletedAsync(payload.Issuer, payload.Subject, cancellationToken)
            .ConfigureAwait(false);
        if (identityOwner is not null && identityOwner.Id != currentUser.Id) {
            return Result.Failure<UserModel>(Errors.Authentication.GoogleIdentityAlreadyLinked);
        }

        currentUser.LinkGoogleIdentity(payload.Issuer, payload.Subject);
        await userContextService.UpdateUserAsync(currentUser, cancellationToken).ConfigureAwait(false);

        return Result.Success(currentUser.ToModel());
    }
}
