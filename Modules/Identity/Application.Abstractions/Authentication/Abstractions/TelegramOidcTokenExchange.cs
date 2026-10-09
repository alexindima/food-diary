namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public sealed record TelegramOidcTokenExchange(
    TelegramAuthorizationCode Code,
    TelegramPkceVerifier CodeVerifier,
    TelegramOidcNonce ExpectedNonce);
