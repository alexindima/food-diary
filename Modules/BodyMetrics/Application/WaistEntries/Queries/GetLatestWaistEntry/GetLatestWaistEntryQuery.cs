using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.GetLatestWaistEntry;

public record GetLatestWaistEntryQuery(
    Guid? UserId
) : IQuery<Result<WaistEntryModel?>>, IUserRequest;
