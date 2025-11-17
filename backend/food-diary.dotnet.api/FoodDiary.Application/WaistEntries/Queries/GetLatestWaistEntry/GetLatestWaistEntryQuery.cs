using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;

public record GetLatestWaistEntryQuery(
    UserId? UserId
) : IQuery<Result<WaistEntryResponse?>>;
