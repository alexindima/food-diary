using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public static class UserErrors {
    public static Error TelegramAlreadyLinked => new(
        "User.TelegramAlreadyLinked",
        "This Telegram identity is already linked to an account.",
        Kind: ErrorKind.Conflict);

    public static Error LastSignInMethod => new(
        "User.LastSignInMethod",
        "Add another sign-in method before disconnecting Telegram.",
        Kind: ErrorKind.Conflict);

    public static Error EmailRequired => new(
        "User.EmailRequired",
        "Add and verify an email address to use this feature.",
        Kind: ErrorKind.Conflict);

    public static Error TelegramIdentityDifferent => new(
        "User.TelegramIdentityDifferent",
        "Disconnect the current Telegram identity before linking another one.",
        Kind: ErrorKind.Conflict);

    public static Error NotFound(Guid id) => new(
        "User.NotFound",
        $"User with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error InvalidPassword => new(
        "User.InvalidPassword",
        "The current password is incorrect.",
        Kind: ErrorKind.Unauthorized);

    public static Error PasswordNotSet => new(
        "User.PasswordNotSet",
        "Password is not configured for this account.",
        Kind: ErrorKind.Conflict);

    public static Error PasswordAlreadySet => new(
        "User.PasswordAlreadySet",
        "Password is already configured for this account.",
        Kind: ErrorKind.Conflict);

    public static Error AdminPasswordResetForbidden => new(
        "User.AdminPasswordResetForbidden",
        "Administrators cannot reset passwords for privileged accounts or their own account.",
        Kind: ErrorKind.Forbidden);

    public static Error NotFound() => new(
        "User.NotFound",
        "User was not found.",
        Kind: ErrorKind.NotFound);

    public static Error InvalidCredentials => new(
        "User.InvalidCredentials",
        "Invalid email or password.",
        Kind: ErrorKind.Unauthorized);

    public static Error EmailAlreadyExists => new(
        "User.EmailAlreadyExists",
        "A user with this email already exists.",
        Kind: ErrorKind.Conflict);
}
