using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Contracts.Queries.ReadRecentHydrationEntries;

public sealed record ReadRecentHydrationEntriesQuery(UserId UserId, DateTime StartUtc, DateTime EndUtc, int Limit) : IQuery<IReadOnlyList<HydrationEntryModel>>;
