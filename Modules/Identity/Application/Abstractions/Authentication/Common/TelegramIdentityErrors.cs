using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public static class TelegramIdentityErrors {
    public static Error TimeZoneRequired => new(
        "Authentication.TelegramTimeZoneRequired",
        "Select a valid IANA time zone to create your diary.",
        Kind: ErrorKind.Validation);

    public static Error InvalidProof => new(
        "Authentication.TelegramInvalidProof",
        "Telegram sign-in could not be verified. Start a new sign-in attempt.",
        Kind: ErrorKind.Unauthorized);

    public static Error NotConfigured => new(
        "Authentication.TelegramOidcNotConfigured",
        "Telegram sign-in is not available.",
        Kind: ErrorKind.Conflict);
}
