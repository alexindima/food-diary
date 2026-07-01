namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Authentication {
        public static Error InvalidCredentials => new(
            "Authentication.InvalidCredentials",
            "Invalid email or password.",
            Kind: ErrorKind.Unauthorized);

        public static Error InvalidToken => new(
            "Authentication.InvalidToken",
            "Invalid authorization token.",
            Kind: ErrorKind.Unauthorized);

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

        public static Error AccountDeleted => new(
            "Authentication.AccountDeleted",
            "Account is scheduled for deletion.",
            Kind: ErrorKind.Unauthorized);

        public static Error AccountNotDeleted => new(
            "Authentication.AccountNotDeleted",
            "Account is already active.",
            Kind: ErrorKind.Conflict);

        public static Error TelegramInvalidData => new(
            "Authentication.TelegramInvalidData",
            "Telegram auth data is invalid.",
            Kind: ErrorKind.Validation);

        public static Error TelegramAuthExpired => new(
            "Authentication.TelegramAuthExpired",
            "Telegram auth data has expired.",
            Kind: ErrorKind.Unauthorized);

        public static Error TelegramNotLinked => new(
            "Authentication.TelegramNotLinked",
            "Telegram account is not linked.",
            Kind: ErrorKind.NotFound);

        public static Error TelegramAlreadyLinked => new(
            "Authentication.TelegramAlreadyLinked",
            "Telegram account is already linked to another user.",
            Kind: ErrorKind.Conflict);

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

        public static Error ImpersonationForbidden => new(
            "Authentication.ImpersonationForbidden",
            "User cannot be impersonated.",
            Kind: ErrorKind.Forbidden);

        public static Error ImpersonationActionForbidden => new(
            "Authentication.ImpersonationActionForbidden",
            "This action is not allowed while impersonating a user.",
            Kind: ErrorKind.Forbidden);
    }
}
