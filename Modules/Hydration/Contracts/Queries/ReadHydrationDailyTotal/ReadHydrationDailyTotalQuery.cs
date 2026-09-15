using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Domain.ValueObjects.Ids;
namespace FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotal;
public sealed record ReadHydrationDailyTotalQuery(UserId UserId, DateTime DateUtc) : IQuery<int>;
