namespace FoodDiary.Modules.Identity.Contracts.Authentication;

public static class IdentityInputLimits {
    public const int MaximumAdminSsoCodeLength = 512;
    public const int MaximumGoogleCredentialLength = 16384;
    public const int MaximumTelegramInitDataLength = 8192;
    public const int MaximumTelegramHashLength = 256;
    public const int MaximumTelegramUsernameLength = 64;
    public const int MaximumTelegramNameLength = 128;
    public const int MaximumTelegramPhotoUrlLength = 2048;
}
