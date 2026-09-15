using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotal;

namespace FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationDailyTotal;

public sealed class ReadHydrationDailyTotalQueryHandler(IHydrationEntryReadModelRepository hydrationEntryReadModelRepository) : IQueryHandler<ReadHydrationDailyTotalQuery, int> {
    public Task<int> Handle(ReadHydrationDailyTotalQuery request, CancellationToken cancellationToken) =>
        hydrationEntryReadModelRepository.GetDailyTotalAsync(request.UserId, request.DateUtc, cancellationToken);
}
