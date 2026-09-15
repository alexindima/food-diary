using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Wearables.Application.Abstractions.Models;

namespace FoodDiary.Modules.Wearables.Application.Commands.SyncWearableData;

public record SyncWearableDataCommand(
    Guid? UserId,
    string Provider,
    DateTime Date) : ICommand<Result<WearableDailySummaryModel>>, IUserRequest;
