namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public sealed record TelegramOidcAuthorizationRequest(
    TelegramOAuthState State,
    TelegramOidcNonce Nonce,
    TelegramPkceVerifier CodeVerifier);
