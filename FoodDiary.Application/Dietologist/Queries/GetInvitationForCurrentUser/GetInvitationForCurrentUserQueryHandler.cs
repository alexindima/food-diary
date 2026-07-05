using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationForCurrentUser;

public sealed class GetInvitationForCurrentUserQueryHandler(
    IDietologistInvitationReadRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService)
    : IQueryHandler<GetInvitationForCurrentUserQuery, Result<DietologistInvitationForCurrentUserModel>> {
    public async Task<Result<DietologistInvitationForCurrentUserModel>> Handle(
        GetInvitationForCurrentUserQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Result<string> userEmailResult = await dietologistUserContextService
            .GetAccessibleUserEmailAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (userEmailResult.IsFailure) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(userEmailResult.Error);
        }

        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(
            new DietologistInvitationId(query.InvitationId),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (invitation is null) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Dietologist.InvitationNotFound);
        }

        if (!string.Equals(invitation.DietologistEmail, userEmailResult.Value, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Dietologist.AccessDenied);
        }

        return Result.Success(invitation.ToCurrentUserInvitationModel());
    }
}
