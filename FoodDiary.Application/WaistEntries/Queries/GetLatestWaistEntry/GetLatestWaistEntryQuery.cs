using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.WaistEntries.Models;

namespace FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;

public record GetLatestWaistEntryQuery(
    Guid? UserId
) : IQuery<Result<WaistEntryModel?>>, IUserRequest;
