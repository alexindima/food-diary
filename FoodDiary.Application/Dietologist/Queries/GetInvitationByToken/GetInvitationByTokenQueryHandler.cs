using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;

public class GetInvitationByTokenQueryHandler(
    IDietologistInvitationRepository invitationRepository)
    : IQueryHandler<GetInvitationByTokenQuery, Result<InvitationModel>> {
    public async Task<Result<InvitationModel>> Handle(GetInvitationByTokenQuery query, CancellationToken cancellationToken) {
        var invitationId = new DietologistInvitationId(query.InvitationId);
        var invitation = await invitationRepository.GetByIdAsync(invitationId, cancellationToken: cancellationToken);

        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure<InvitationModel>(Errors.Dietologist.InvitationNotFound);
        }

        if (invitation.IsExpired()) {
            return Result.Failure<InvitationModel>(Errors.Dietologist.InvitationExpired);
        }

        return Result.Success(invitation.ToInvitationModel());
    }
}
