using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.ListReadyTelegramOperations;

public sealed class ListReadyTelegramOperationsQueryHandler(TelegramOperationService service)
    : IQueryHandler<ListReadyTelegramOperationsQuery, Result<IReadOnlyList<Guid>>> {
    public Task<Result<IReadOnlyList<Guid>>> Handle(ListReadyTelegramOperationsQuery query, CancellationToken cancellationToken) =>
        service.ListReadyAsync(cancellationToken);
}
