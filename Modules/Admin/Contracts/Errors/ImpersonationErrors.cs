using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Contracts.Errors;

public static class ImpersonationErrors {
    public static Error ImpersonationForbidden => new(
        "Authentication.ImpersonationForbidden",
        "User cannot be impersonated.",
        Kind: ErrorKind.Forbidden);

    public static Error ImpersonationActionForbidden => new(
        "Authentication.ImpersonationActionForbidden",
        "This action is not allowed while impersonating a user.",
        Kind: ErrorKind.Forbidden);
}
