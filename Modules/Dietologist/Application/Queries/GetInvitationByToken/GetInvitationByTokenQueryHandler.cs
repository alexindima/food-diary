using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Application.Common.Validation;
using FoodDiary.Modules.Dietologist.Application.Mappings;
using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetInvitationByToken;

public sealed class GetInvitationByTokenQueryHandler(
    IDietologistInvitationReadModelRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    TimeProvider timeProvider,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetInvitationByTokenQuery, Result<InvitationModel>> {
    public async Task<Result<InvitationModel>> Handle(GetInvitationByTokenQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<InvitationModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Guid invitationId = query.InvitationId;
        Result<DietologistInvitationId> invitationIdResult = ParseInvitationId(invitationId);
        if (invitationIdResult.IsFailure) {
            return DietologistRequiredIdParser.ToFailure<InvitationModel, DietologistInvitationId>(invitationIdResult);
        }

        DietologistInvitationReadModel? invitation = await invitationRepository.GetByIdReadModelAsync(invitationIdResult.Value, cancellationToken).ConfigureAwait(false);

        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure<InvitationModel>(DietologistErrors.InvitationNotFound);
        }

        string? userEmail = await dietologistUserContextService.GetUserEmailByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userEmail is null ||
            !string.Equals(invitation.DietologistEmail, userEmail, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<InvitationModel>(DietologistErrors.InvitationNotFound);
        }

        if (IsExpired(invitation)) {
            return Result.Failure<InvitationModel>(DietologistErrors.InvitationExpired);
        }

        return Result.Success(invitation.ToInvitationModel());

    }

    private static Result<DietologistInvitationId> ParseInvitationId(Guid invitationId) =>
        DietologistRequiredIdParser.Parse(
            invitationId,
            nameof(invitationId),
            "Invitation id must not be empty.",
            value => new DietologistInvitationId(value));

    private bool IsExpired(DietologistInvitationReadModel invitation) =>
        invitation.Status == DietologistInvitationStatus.Pending &&
        invitation.ExpiresAtUtc <= timeProvider.GetUtcNow().UtcDateTime;

}
