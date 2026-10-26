namespace FoodDiary.Modules.Identity.Contracts.Authentication.Common;

public static class BrowserSessionCredential {
    public static string Create(Guid sessionId) => $"{sessionId:N}.{SecurityTokenGenerator.GenerateUrlSafeToken()}";

    public static bool TryReadSessionId(string? credential, out Guid sessionId) {
        sessionId = Guid.Empty;
        return credential is { Length: 76 } && credential[32] == '.' &&
               Guid.TryParseExact(credential.AsSpan(0, 32), "N", out sessionId) && sessionId != Guid.Empty &&
               credential.AsSpan(33).IndexOfAnyExcept("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_".AsSpan()) < 0;
    }
}
