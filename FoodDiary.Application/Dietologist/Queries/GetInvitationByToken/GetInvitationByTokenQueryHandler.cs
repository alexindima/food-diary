using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;

public sealed class GetInvitationByTokenQueryHandler(
    IDietologistInvitationReadRepository invitationRepository,
    IDietologistUserContextService userContextService)
    : IQueryHandler<GetInvitationByTokenQuery, Result<InvitationModel>> {
    public async Task<Result<InvitationModel>> Handle(GetInvitationByTokenQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<InvitationModel>(userIdResult.Error);
        }

        var invitationId = new DietologistInvitationId(query.InvitationId);
        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(invitationId, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure<InvitationModel>(Errors.Dietologist.InvitationNotFound);
        }

        string? userEmail = await userContextService.GetUserEmailByIdAsync(userIdResult.Value, cancellationToken).ConfigureAwait(false);
        if (userEmail is null ||
            !string.Equals(invitation.DietologistEmail, userEmail, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<InvitationModel>(Errors.Dietologist.InvitationNotFound);
        }

        if (invitation.IsExpired()) {
            return Result.Failure<InvitationModel>(Errors.Dietologist.InvitationExpired);
        }

        return Result.Success(invitation.ToInvitationModel());
    }
}
