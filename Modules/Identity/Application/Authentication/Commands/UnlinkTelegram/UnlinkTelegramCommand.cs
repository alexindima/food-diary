using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.UnlinkTelegram;

public sealed record UnlinkTelegramCommand(Guid? UserId, string InitData) : ICommand<Result>, IUserRequest;
