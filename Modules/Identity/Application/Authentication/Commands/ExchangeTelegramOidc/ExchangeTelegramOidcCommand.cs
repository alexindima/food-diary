using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.ExchangeTelegramOidc;

public sealed record ExchangeTelegramOidcCommand(string Code, string State, string BrowserBinding) : ICommand<Result<TelegramAuthenticationIntentModel>>;
