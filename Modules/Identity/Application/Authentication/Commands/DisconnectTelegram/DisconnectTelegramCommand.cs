using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.DisconnectTelegram;

public sealed record DisconnectTelegramCommand(Guid? UserId, long? SecurityVersion) : ICommand<Result>, IUserRequest;
