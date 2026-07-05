using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetMyClients;

public sealed class GetMyClientsQueryHandler(IDietologistInvitationReadService readService)
    : IQueryHandler<GetMyClientsQuery, Result<IReadOnlyList<ClientSummaryModel>>> {
    public async Task<Result<IReadOnlyList<ClientSummaryModel>>> Handle(GetMyClientsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<ClientSummaryModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        return await readService.GetMyClientsAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
