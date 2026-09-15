using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationInterval;

namespace FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationInterval;

public sealed class ReadHydrationIntervalQueryHandler(IHydrationIntervalReadModelRepository repository) : IQueryHandler<ReadHydrationIntervalQuery, long> {
    public Task<long> Handle(ReadHydrationIntervalQuery request, CancellationToken cancellationToken) => repository.GetTotalAsync(request.UserId, request.StartUtc, request.EndExclusiveUtc, cancellationToken);
}
