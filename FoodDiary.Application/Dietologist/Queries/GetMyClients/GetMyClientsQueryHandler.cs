using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetMyClients;

public sealed class GetMyClientsQueryHandler(
    IDietologistInvitationReadService readService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMyClientsQuery, Result<IReadOnlyList<ClientSummaryModel>>> {
    public async Task<Result<IReadOnlyList<ClientSummaryModel>>> Handle(GetMyClientsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<ClientSummaryModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await readService.GetMyClientsAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
