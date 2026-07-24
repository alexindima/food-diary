using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.SetAttentionSignalState;

public sealed record SetAttentionSignalStateCommand(
    Guid? UserId,
    Guid ClientUserId,
    string SignalId,
    string Action,
    DateTime? SnoozedUntilUtc)
    : ICommand<Result>, IUserRequest;
