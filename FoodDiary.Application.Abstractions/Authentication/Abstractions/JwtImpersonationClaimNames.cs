namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public static class JwtImpersonationClaimNames {
    public const string IsImpersonation = "fd_impersonation";
    public const string ActorUserId = "fd_impersonated_by";
    public const string Reason = "fd_impersonation_reason";
}
