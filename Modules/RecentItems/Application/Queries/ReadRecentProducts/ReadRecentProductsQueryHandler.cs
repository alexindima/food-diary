using FoodDiary.Mediator;
using FoodDiary.Modules.RecentItems.Application.Abstractions.Common;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentProducts;

namespace FoodDiary.Modules.RecentItems.Application.Queries.ReadRecentProducts;

public sealed class ReadRecentProductsQueryHandler(IRecentItemReadRepository repository) : IRequestHandler<ReadRecentProductsQuery, IReadOnlyList<RecentProductUsage>> {
    public Task<IReadOnlyList<RecentProductUsage>> Handle(ReadRecentProductsQuery request, CancellationToken cancellationToken) =>
        repository.GetRecentProductsAsync(request.UserId, request.Limit, cancellationToken);
}
