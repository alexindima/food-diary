using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.StartTelegramOidc;

public sealed record StartTelegramOidcCommand(string BrowserBinding, Guid? LinkUserId = null) : ICommand<Result<TelegramOidcStartModel>>;
