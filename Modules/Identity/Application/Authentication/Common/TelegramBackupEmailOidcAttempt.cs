namespace FoodDiary.Modules.Identity.Application.Authentication.Common;

internal sealed record TelegramBackupEmailOidcAttempt(Guid UserId, string Email, long SecurityVersion, string Nonce, string CodeVerifier) {
    internal const string Purpose = "telegram-backup-email-oidc";
    internal static string Binding(string browserBinding, Guid userId) => $"{browserBinding}:{userId:D}";
}
