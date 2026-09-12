using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.StartTelegramOidc;

public sealed record StartTelegramOidcCommand(string BrowserBinding, Guid? LinkUserId = null) : ICommand<Result<TelegramOidcStartModel>>;
