using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.UnlinkTelegram;

public sealed record UnlinkTelegramCommand(Guid? UserId, string InitData) : ICommand<Result>, IUserRequest;
