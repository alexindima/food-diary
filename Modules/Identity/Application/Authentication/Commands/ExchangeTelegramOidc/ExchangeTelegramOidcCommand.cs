using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.ExchangeTelegramOidc;

public sealed record ExchangeTelegramOidcCommand(string Code, string State, string BrowserBinding) : ICommand<Result<TelegramAuthenticationIntentModel>>;
