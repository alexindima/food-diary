using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationForCurrentUser;

public sealed class GetInvitationForCurrentUserQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetInvitationForCurrentUserQuery, Result<DietologistInvitationForCurrentUserModel>> {
    public async Task<Result<DietologistInvitationForCurrentUserModel>> Handle(
        GetInvitationForCurrentUserQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(accessError);
        }

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Authentication.InvalidToken);
        }

        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(
            new DietologistInvitationId(query.InvitationId),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (invitation is null) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Dietologist.InvitationNotFound);
        }

        if (!string.Equals(invitation.DietologistEmail, user.Email, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Dietologist.AccessDenied);
        }

        return Result.Success(invitation.ToCurrentUserInvitationModel());
    }
}
