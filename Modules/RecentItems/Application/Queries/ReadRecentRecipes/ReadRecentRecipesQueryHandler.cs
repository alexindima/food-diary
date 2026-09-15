using FoodDiary.Mediator;
using FoodDiary.Modules.RecentItems.Application.Abstractions.Common;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentRecipes;

namespace FoodDiary.Modules.RecentItems.Application.Queries.ReadRecentRecipes;

public sealed class ReadRecentRecipesQueryHandler(IRecentItemReadRepository repository) : IRequestHandler<ReadRecentRecipesQuery, IReadOnlyList<RecentRecipeUsage>> {
    public Task<IReadOnlyList<RecentRecipeUsage>> Handle(ReadRecentRecipesQuery request, CancellationToken cancellationToken) =>
        repository.GetRecentRecipesAsync(request.UserId, request.Limit, cancellationToken);
}
