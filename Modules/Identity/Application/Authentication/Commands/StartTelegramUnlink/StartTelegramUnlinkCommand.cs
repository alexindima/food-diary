using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.StartTelegramUnlink;

public sealed record StartTelegramUnlinkCommand(Guid? UserId, string BrowserBinding)
    : ICommand<Result<TelegramOidcStartModel>>, IUserRequest;
