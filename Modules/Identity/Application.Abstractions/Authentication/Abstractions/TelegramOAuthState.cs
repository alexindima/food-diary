namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public sealed record TelegramOAuthState(string Value) {
    public override string ToString() => nameof(TelegramOAuthState);
}
