using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Пользователь системы - корень агрегата
/// </summary>
public sealed class User : AggregateRoot<UserId> {
    private const long DefaultAiInputTokenLimit = 5_000_000;
    private const long DefaultAiOutputTokenLimit = 1_000_000;

    public string Email { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;
    public string? RefreshToken { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public string? EmailConfirmationTokenHash { get; private set; }
    public DateTime? EmailConfirmationTokenExpiresAtUtc { get; private set; }
    public DateTime? EmailConfirmationSentAtUtc { get; private set; }
    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; private set; }
    public DateTime? PasswordResetSentAtUtc { get; private set; }
    public string? Username { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public DateTime? BirthDate { get; private set; }
    public string? Gender { get; private set; }
    public double? Weight { get; private set; }
    public double? DesiredWeight { get; private set; }
    public double? DesiredWaist { get; private set; }
    public double? Height { get; private set; }
    public ActivityLevel ActivityLevel { get; private set; } = ActivityLevel.Moderate;
    public double? DailyCalorieTarget { get; private set; }
    public double? ProteinTarget { get; private set; }
    public double? FatTarget { get; private set; }
    public double? CarbTarget { get; private set; }
    public double? FiberTarget { get; private set; }
    public int? StepGoal { get; private set; }
    public double? WaterGoal { get; private set; }
    public double? HydrationGoal { get; private set; }
    public string? ProfileImage { get; private set; }
    public ImageAssetId? ProfileImageAssetId { get; private set; }
    public string? DashboardLayoutJson { get; private set; }
    public string? Language { get; private set; }
    public long? TelegramUserId { get; private set; }
    public long AiInputTokenLimit { get; private set; } = DefaultAiInputTokenLimit;
    public long AiOutputTokenLimit { get; private set; } = DefaultAiOutputTokenLimit;
    public bool IsActive { get; private set; } = true;
    public DateTime? DeletedAt { get; private set; }

    // Navigation properties
    public ICollection<Meal> Meals { get; private set; } = new List<Meal>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();
    public ICollection<Recipe> Recipes { get; private set; } = new List<Recipe>();
    public ICollection<WeightEntry> WeightEntries { get; private set; } = new List<WeightEntry>();
    public ICollection<WaistEntry> WaistEntries { get; private set; } = new List<WaistEntry>();
    public ICollection<Cycle> Cycles { get; private set; } = new List<Cycle>();
    public ICollection<HydrationEntry> HydrationEntries { get; private set; } = new List<HydrationEntry>();
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    // Конструктор для EF Core
    private User() {
    }

    // Factory method для создания нового пользователя
    public static User Create(string email, string hashedPassword) {
        var user = new User {
            Id = UserId.New(),
            Email = email,
            Password = hashedPassword,
            AiInputTokenLimit = DefaultAiInputTokenLimit,
            AiOutputTokenLimit = DefaultAiOutputTokenLimit,
            IsEmailConfirmed = false
        };
        user.SetCreated();
        return user;
    }

    public void UpdateRefreshToken(string? refreshToken) {
        RefreshToken = refreshToken;
        SetModified();
    }

    public void LinkTelegram(long telegramUserId)
    {
        TelegramUserId = telegramUserId;
        SetModified();
    }

    public void UnlinkTelegram()
    {
        TelegramUserId = null;
        SetModified();
    }

    public void UpdatePassword(string hashedPassword) {
        Password = hashedPassword;
        SetModified();
    }

    public void SetEmailConfirmationToken(string tokenHash, DateTime expiresAtUtc)
    {
        EmailConfirmationTokenHash = tokenHash;
        EmailConfirmationTokenExpiresAtUtc = expiresAtUtc;
        EmailConfirmationSentAtUtc = DateTime.UtcNow;
        SetModified();
    }

    public void ConfirmEmail()
    {
        SetEmailConfirmed(true);
    }

    public void SetEmailConfirmed(bool isConfirmed)
    {
        IsEmailConfirmed = isConfirmed;
        EmailConfirmationTokenHash = null;
        EmailConfirmationTokenExpiresAtUtc = null;
        EmailConfirmationSentAtUtc = null;
        SetModified();
    }

    public void SetPasswordResetToken(string tokenHash, DateTime expiresAtUtc)
    {
        PasswordResetTokenHash = tokenHash;
        PasswordResetTokenExpiresAtUtc = expiresAtUtc;
        PasswordResetSentAtUtc = DateTime.UtcNow;
        SetModified();
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAtUtc = null;
        PasswordResetSentAtUtc = null;
        SetModified();
    }

    public void UpdateProfile(
        string? username = null,
        string? firstName = null,
        string? lastName = null,
        DateTime? birthDate = null,
        string? gender = null,
        double? weight = null,
        double? circumference = null,
        double? height = null,
        ActivityLevel? activityLevel = null,
        double? dailyCalorieTarget = null,
        double? proteinTarget = null,
        double? fatTarget = null,
        double? carbTarget = null,
        double? fiberTarget = null,
        int? stepGoal = null,
        double? waterGoal = null,
        double? hydrationGoal = null,
        string? profileImage = null,
        ImageAssetId? profileImageAssetId = null,
        string? dashboardLayoutJson = null,
        string? language = null) {
        if (username is not null) Username = username;
        if (firstName is not null) FirstName = firstName;
        if (lastName is not null) LastName = lastName;
        if (birthDate.HasValue) BirthDate = birthDate;
        if (gender is not null) Gender = gender;
        if (weight.HasValue) Weight = weight;
        if (circumference.HasValue) DesiredWaist = circumference;
        if (height.HasValue) Height = height;
        if (activityLevel.HasValue) ActivityLevel = activityLevel.Value;
        if (dailyCalorieTarget.HasValue) DailyCalorieTarget = dailyCalorieTarget;
        if (proteinTarget.HasValue) ProteinTarget = proteinTarget;
        if (fatTarget.HasValue) FatTarget = fatTarget;
        if (carbTarget.HasValue) CarbTarget = carbTarget;
        if (fiberTarget.HasValue) FiberTarget = fiberTarget;
        if (stepGoal.HasValue) StepGoal = stepGoal;
        if (waterGoal.HasValue) WaterGoal = waterGoal;
        if (hydrationGoal.HasValue) HydrationGoal = hydrationGoal;
        if (profileImage is not null) ProfileImage = profileImage;
        if (profileImageAssetId.HasValue) ProfileImageAssetId = profileImageAssetId;
        if (dashboardLayoutJson is not null) DashboardLayoutJson = dashboardLayoutJson;
        if (language is not null) Language = language;

        SetModified();
    }

    public void UpdateAiTokenLimits(long? inputLimit, long? outputLimit)
    {
        if (inputLimit.HasValue)
        {
            AiInputTokenLimit = inputLimit.Value;
        }

        if (outputLimit.HasValue)
        {
            AiOutputTokenLimit = outputLimit.Value;
        }

        SetModified();
    }

    public void UpdateDesiredWeight(double? desiredWeight)
    {
        DesiredWeight = desiredWeight;
        SetModified();
    }

    public void UpdateDesiredWaist(double? desiredWaist)
    {
        DesiredWaist = desiredWaist;
        SetModified();
    }

    public void Deactivate() {
        IsActive = false;
        SetModified();
    }

    public void Activate() {
        IsActive = true;
        SetModified();
    }

    public void MarkDeleted(DateTime deletedAtUtc)
    {
        DeletedAt = deletedAtUtc;
        IsActive = false;
        SetModified();
    }

    public void Restore()
    {
        DeletedAt = null;
        IsActive = true;
        SetModified();
    }
}
