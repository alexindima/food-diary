using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Queries.ListReadyTelegramOperations;

public sealed class ListReadyTelegramOperationsQueryHandler(TelegramOperationService service)
    : IQueryHandler<ListReadyTelegramOperationsQuery, Result<IReadOnlyList<Guid>>> {
    public Task<Result<IReadOnlyList<Guid>>> Handle(ListReadyTelegramOperationsQuery query, CancellationToken cancellationToken) =>
        service.ListReadyAsync(cancellationToken);
}
