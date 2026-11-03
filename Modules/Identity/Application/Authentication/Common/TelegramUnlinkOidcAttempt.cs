namespace FoodDiary.Modules.Identity.Application.Authentication.Common;

internal sealed record TelegramUnlinkOidcAttempt(Guid UserId, long SecurityVersion, string Nonce, string CodeVerifier) {
    internal const string Purpose = "telegram-unlink-oidc";
    internal static string Binding(string browserBinding, Guid userId) => $"{browserBinding}:{userId:D}";
}
