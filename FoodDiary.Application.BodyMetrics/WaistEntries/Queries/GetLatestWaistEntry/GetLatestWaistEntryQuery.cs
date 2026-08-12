using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.WaistEntries.Models;

namespace FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;

public record GetLatestWaistEntryQuery(
    Guid? UserId
) : IQuery<Result<WaistEntryModel?>>, IUserRequest;
