using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RevokeSession;

public sealed record RevokeSessionCommand(Guid UserId, Guid CurrentSessionId, Guid SessionId) : ICommand<Result>;
