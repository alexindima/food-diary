using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
namespace FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotal;
public sealed record ReadHydrationDailyTotalQuery(UserId UserId, DateTime DateUtc) : IQuery<int>;
