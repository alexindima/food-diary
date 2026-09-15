using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
namespace FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationEntries;
public sealed record ReadHydrationEntriesQuery(UserId UserId, DateTime DateUtc) : IQuery<IReadOnlyList<HydrationEntryModel>>;
