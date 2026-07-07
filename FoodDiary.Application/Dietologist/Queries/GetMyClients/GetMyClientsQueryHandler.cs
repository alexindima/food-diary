using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetMyClients;

public sealed class GetMyClientsQueryHandler(IDietologistInvitationReadService readService)
    : IQueryHandler<GetMyClientsQuery, Result<IReadOnlyList<ClientSummaryModel>>> {
    public async Task<Result<IReadOnlyList<ClientSummaryModel>>> Handle(GetMyClientsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<ClientSummaryModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await readService.GetMyClientsAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
