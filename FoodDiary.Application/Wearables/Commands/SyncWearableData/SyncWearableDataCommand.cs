using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Wearables.Models;

namespace FoodDiary.Application.Wearables.Commands.SyncWearableData;

public record SyncWearableDataCommand(
    Guid? UserId,
    string Provider,
    DateTime Date) : ICommand<Result<WearableDailySummaryModel>>, IUserRequest;
