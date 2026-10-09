namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public sealed record TelegramPkceVerifier(string Value) {
    public override string ToString() => nameof(TelegramPkceVerifier);
}
