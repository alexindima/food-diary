using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Queries.GetMyDietologist;

public class GetMyDietologistQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMyDietologistQuery, Result<DietologistInfoModel?>> {
    public async Task<Result<DietologistInfoModel?>> Handle(GetMyDietologistQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DietologistInfoModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<DietologistInfoModel?>(accessError);
        }

        DietologistInvitation? invitation = await invitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success(invitation?.ToDietologistInfoModel());
    }
}
