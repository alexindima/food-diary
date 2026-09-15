using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Application.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetMyDietologist;

public sealed class GetMyDietologistQueryHandler(
    IDietologistInvitationReadModelRepository invitationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMyDietologistQuery, Result<DietologistInfoModel?>> {
    public async Task<Result<DietologistInfoModel?>> Handle(GetMyDietologistQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DietologistInfoModel?>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        DietologistInvitationReadModel? invitation = await invitationRepository.GetActiveByClientReadModelAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(invitation is null ? null : invitation.ToDietologistInfoModel());

    }

}
