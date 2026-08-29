using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.RevokeOtherSessions;

public sealed record RevokeOtherSessionsCommand(Guid UserId, Guid CurrentSessionId) : ICommand<Result>;
