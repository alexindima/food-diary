using FoodDiary.Mediator;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentProducts;

public sealed record ReadRecentProductsQuery(UserId UserId, int Limit) : IRequest<IReadOnlyList<RecentProductUsage>>;
