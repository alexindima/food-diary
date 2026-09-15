using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
namespace FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationInterval;
public sealed record ReadHydrationIntervalQuery(UserId UserId, DateTime StartUtc, DateTime EndExclusiveUtc) : IQuery<long>;
