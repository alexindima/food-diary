namespace FoodDiary.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string SmtpUser { get; init; } = string.Empty;
    public string SmtpPassword { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "FoodDiary";
    public string FrontendBaseUrl { get; init; } = string.Empty;
    public string VerificationPath { get; init; } = "/verify-email";
    public string PasswordResetPath { get; init; } = "/reset-password";
}
