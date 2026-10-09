namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public sealed record TelegramAuthorizationCode(string Value) {
    public override string ToString() => nameof(TelegramAuthorizationCode);
}
