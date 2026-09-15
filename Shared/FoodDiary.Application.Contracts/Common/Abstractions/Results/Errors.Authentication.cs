using FoodDiary.Results;

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

        public static Error GoogleAccountLinkRequired => new(
            "Authentication.GoogleAccountLinkRequired",
            "Sign in with your existing account before linking Google.",
            Kind: ErrorKind.Conflict);

        public static Error GoogleAccountEmailMismatch => new(
            "Authentication.GoogleAccountEmailMismatch",
            "The Google account email must match your FoodDiary account email.",
            Kind: ErrorKind.Conflict);

        public static Error GoogleIdentityAlreadyLinked => new(
            "Authentication.GoogleIdentityAlreadyLinked",
            "This Google account is already linked to another FoodDiary account.",
            Kind: ErrorKind.Conflict);

        public static Error GoogleIdentityDifferent => new(
            "Authentication.GoogleIdentityDifferent",
            "A different Google account is already linked to this FoodDiary account.",
            Kind: ErrorKind.Conflict);

        public static Error AccountDeleted => new(
            "Authentication.AccountDeleted",
            "Account is scheduled for deletion.",
            Kind: ErrorKind.Unauthorized);

        public static Error AccountNotDeleted => new(
            "Authentication.AccountNotDeleted",
            "Account is already active.",
            Kind: ErrorKind.Conflict);

        public static Error TelegramNotLinked => new(
            "Authentication.TelegramNotLinked",
            "Telegram account is not linked.",
            Kind: ErrorKind.NotFound);

        public static Error TelegramAlreadyLinked => new(
            "Authentication.TelegramAlreadyLinked",
            "Telegram account is already linked to another user.",
            Kind: ErrorKind.Conflict);

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
