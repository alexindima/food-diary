namespace FoodDiary.Application.Abstractions.Authentication.Common;

public static class AuthenticationInputLimits {
    public const int MinimumPasswordLength = 6;
    public const int MaximumPasswordLength = 256;
    public const int MaximumOpaqueTokenLength = 4096;
    public const int MaximumAdminSsoCodeLength = 512;
    public const int MaximumGoogleCredentialLength = 16384;
    public const int MaximumTelegramInitDataLength = 8192;
    public const int MaximumTelegramHashLength = 256;
    public const int MaximumTelegramUsernameLength = 64;
    public const int MaximumTelegramNameLength = 128;
    public const int MaximumTelegramPhotoUrlLength = 2048;
}
