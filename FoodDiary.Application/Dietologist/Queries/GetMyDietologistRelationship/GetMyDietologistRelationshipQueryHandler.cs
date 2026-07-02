using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Queries.GetMyDietologistRelationship;

public class GetMyDietologistRelationshipQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMyDietologistRelationshipQuery, Result<DietologistRelationshipModel?>> {
    public async Task<Result<DietologistRelationshipModel?>> Handle(
        GetMyDietologistRelationshipQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DietologistRelationshipModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<DietologistRelationshipModel?>(accessError);
        }

        DietologistInvitation? accepted = await invitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (accepted is not null) {
            return Result.Success<DietologistRelationshipModel?>(accepted.ToRelationshipModel());
        }

        DietologistInvitation? pending = await invitationRepository.GetByClientAndStatusAsync(
            userId,
            DietologistInvitationStatus.Pending,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return Result.Success(pending?.ToRelationshipModel());
    }
}
