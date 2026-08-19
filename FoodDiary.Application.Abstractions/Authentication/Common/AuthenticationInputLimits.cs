namespace FoodDiary.Application.Abstractions.Authentication.Common;

public static class AuthenticationInputLimits {
    public const int MinimumPasswordLength = 6;
    public const int MaximumPasswordLength = 256;
    public const int MaximumOpaqueTokenLength = 4096;
}
