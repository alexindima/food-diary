using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Application.Common.Validation;
using FoodDiary.Modules.Dietologist.Application.Mappings;
using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetInvitationForCurrentUser;

public sealed class GetInvitationForCurrentUserQueryHandler(
    IDietologistInvitationReadModelRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    TimeProvider timeProvider,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetInvitationForCurrentUserQuery, Result<DietologistInvitationForCurrentUserModel>> {
    public async Task<Result<DietologistInvitationForCurrentUserModel>> Handle(
        GetInvitationForCurrentUserQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DietologistInvitationForCurrentUserModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Guid invitationId = query.InvitationId;
        Result<UserDietologistProfileModel> userResult = await dietologistUserContextService
            .GetAccessibleProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(userResult.Error);
        }
        if (!userResult.Value.IsEmailConfirmed) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(DietologistErrors.AccessDenied);
        }

        Result<DietologistInvitationId> invitationIdResult = ParseInvitationId(invitationId);
        if (invitationIdResult.IsFailure) {
            return DietologistRequiredIdParser.ToFailure<DietologistInvitationForCurrentUserModel, DietologistInvitationId>(invitationIdResult);
        }

        DietologistInvitationReadModel? invitation = await invitationRepository.GetByIdReadModelAsync(
            invitationIdResult.Value,
            cancellationToken).ConfigureAwait(false);
        if (invitation is null) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(DietologistErrors.InvitationNotFound);
        }

        if (!string.Equals(invitation.DietologistEmail, userResult.Value.Email, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(DietologistErrors.AccessDenied);
        }

        return Result.Success(invitation.ToCurrentUserInvitationModel(timeProvider));

    }

    private static Result<DietologistInvitationId> ParseInvitationId(Guid invitationId) =>
        DietologistRequiredIdParser.Parse(
            invitationId,
            nameof(invitationId),
            "Invitation id must not be empty.",
            value => new DietologistInvitationId(value));

}
