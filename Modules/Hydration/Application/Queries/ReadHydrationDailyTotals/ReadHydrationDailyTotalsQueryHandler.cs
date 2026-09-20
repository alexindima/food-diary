using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotals;

namespace FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationDailyTotals;

public sealed class ReadHydrationDailyTotalsQueryHandler(IHydrationEntryReadModelRepository hydrationEntryReadModelRepository) : IQueryHandler<ReadHydrationDailyTotalsQuery, IReadOnlyList<(DateTime Date, int TotalMl)>> {
    public Task<IReadOnlyList<(DateTime Date, int TotalMl)>> Handle(ReadHydrationDailyTotalsQuery request, CancellationToken cancellationToken) =>
        hydrationEntryReadModelRepository.GetDailyTotalsAsync(request.UserId, request.DateFrom, request.DateTo, cancellationToken, request.UseExactBounds);
}
