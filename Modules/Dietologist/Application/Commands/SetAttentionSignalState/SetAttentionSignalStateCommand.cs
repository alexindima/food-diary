using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.SetAttentionSignalState;

public sealed record SetAttentionSignalStateCommand(
    Guid? UserId,
    Guid ClientUserId,
    string SignalId,
    string Action,
    DateTime? SnoozedUntilUtc)
    : ICommand<Result>, IUserRequest;
