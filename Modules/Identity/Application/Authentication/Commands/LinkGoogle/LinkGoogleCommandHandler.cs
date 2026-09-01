using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.LinkGoogle;

public sealed class LinkGoogleCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
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

        GoogleIdentityPayload payload = payloadResult.Value;
        return await userIdentityService.LinkGoogleAsync(
            userIdResult.Value,
            payload.Email,
            payload.Issuer,
            payload.Subject,
            cancellationToken).ConfigureAwait(false);
    }
}
