using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RevokeOtherSessions;

public sealed record RevokeOtherSessionsCommand(Guid UserId, Guid CurrentSessionId) : ICommand<Result>;
