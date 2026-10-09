using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RevokeOtherSessions;

public sealed record RevokeOtherSessionsCommand(UserId UserId, RefreshTokenSessionId CurrentSessionId) : ICommand<Result>;
