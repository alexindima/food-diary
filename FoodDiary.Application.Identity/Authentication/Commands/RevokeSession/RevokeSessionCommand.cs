using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.RevokeSession;

public sealed record RevokeSessionCommand(Guid UserId, Guid CurrentSessionId, Guid SessionId) : ICommand<Result>;
