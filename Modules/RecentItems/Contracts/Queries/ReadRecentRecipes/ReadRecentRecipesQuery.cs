using FoodDiary.Mediator;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentRecipes;

public sealed record ReadRecentRecipesQuery(UserId UserId, int Limit) : IRequest<IReadOnlyList<RecentRecipeUsage>>;
