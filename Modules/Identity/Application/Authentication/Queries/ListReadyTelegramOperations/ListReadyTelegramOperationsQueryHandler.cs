using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.ListReadyTelegramOperations;

public sealed class ListReadyTelegramOperationsQueryHandler(ITelegramOperationStore store, ITelegramOperationPolicy policy)
    : IQueryHandler<ListReadyTelegramOperationsQuery, Result<IReadOnlyList<TelegramOperationId>>> {
    public async Task<Result<IReadOnlyList<TelegramOperationId>>> Handle(ListReadyTelegramOperationsQuery query, CancellationToken cancellationToken) =>
        TelegramOperationChecks.IsEnabled(policy)
            ? Result.Success(await store.ListReadyAsync(policy.BotId, cancellationToken).ConfigureAwait(false))
            : Result.Failure<IReadOnlyList<TelegramOperationId>>(TelegramOperationChecks.Unavailable);
}
