using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Contracts.Errors;

public static class IdentityErrors {
    public static Error GoogleInvalidToken => new(
        "Authentication.GoogleInvalidToken",
        "Google credential is invalid.",
        Kind: ErrorKind.Unauthorized);

    public static Error GoogleNotConfigured => new(
        "Authentication.GoogleNotConfigured",
        "Google authentication is not configured.",
        Kind: ErrorKind.Internal);

    public static Error GoogleEmailNotVerified => new(
        "Authentication.GoogleEmailNotVerified",
        "Google account email is not verified.",
        Kind: ErrorKind.Unauthorized);

    public static Error TelegramInvalidData => new(
        "Authentication.TelegramInvalidData",
        "Telegram auth data is invalid.",
        Kind: ErrorKind.Validation);

    public static Error TelegramAuthExpired => new(
        "Authentication.TelegramAuthExpired",
        "Telegram auth data has expired.",
        Kind: ErrorKind.Unauthorized);

    public static Error TelegramAssertionAlreadyUsed => new(
        "Authentication.TelegramAssertionAlreadyUsed",
        "Telegram authentication data has already been used.",
        Kind: ErrorKind.Unauthorized);

    public static Error TelegramNotConfigured => new(
        "Authentication.TelegramNotConfigured",
        "Telegram authentication is not configured.",
        Kind: ErrorKind.Internal);

    public static Error TelegramBotNotConfigured => new(
        "Authentication.TelegramBotNotConfigured",
        "Telegram bot authentication is not configured.",
        Kind: ErrorKind.Internal);

    public static Error TelegramBotInvalidSecret => new(
        "Authentication.TelegramBotInvalidSecret",
        "Telegram bot secret is invalid.",
        Kind: ErrorKind.Unauthorized);

    public static Error AdminSsoInvalidCode => new(
        "Authentication.AdminSsoInvalidCode",
        "Admin SSO code is invalid or expired.",
        Kind: ErrorKind.Unauthorized);

    public static Error AdminSsoForbidden => new(
        "Authentication.AdminSsoForbidden",
        "User is not allowed to access admin SSO.",
        Kind: ErrorKind.Forbidden);
}
