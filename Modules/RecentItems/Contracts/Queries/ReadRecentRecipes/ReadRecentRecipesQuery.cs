using FoodDiary.Mediator;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentRecipes;

public sealed record ReadRecentRecipesQuery(UserId UserId, int Limit) : IRequest<IReadOnlyList<RecentRecipeUsage>>;
