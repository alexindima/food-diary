using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Domain.ValueObjects.Ids;
namespace FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotals;
public sealed record ReadHydrationDailyTotalsQuery(UserId UserId, DateTime DateFrom, DateTime DateTo) : IQuery<IReadOnlyList<(DateTime Date, int TotalMl)>>;
