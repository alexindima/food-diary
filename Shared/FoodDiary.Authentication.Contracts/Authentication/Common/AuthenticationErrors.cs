using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public static class AuthenticationErrors {
    public static Error InvalidCredentials => new(
        "Authentication.InvalidCredentials",
        "Invalid email or password.",
        Kind: ErrorKind.Unauthorized);

    public static Error InvalidToken => new(
        "Authentication.InvalidToken",
        "Invalid authorization token.",
        Kind: ErrorKind.Unauthorized);
}
