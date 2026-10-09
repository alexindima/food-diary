namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public sealed record TelegramOidcNonce(string Value) {
    public override string ToString() => nameof(TelegramOidcNonce);
}
