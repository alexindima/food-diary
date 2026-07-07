using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public static class UserErrors {
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
