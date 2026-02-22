using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Users;

/// <summary>
/// ÐŸÐ¾Ð»ÑŒÐ·Ð¾Ð²Ð°Ñ‚ÐµÐ»ÑŒ ÑÐ¸ÑÑ‚ÐµÐ¼Ñ‹ - ÐºÐ¾Ñ€ÐµÐ½ÑŒ Ð°Ð³Ñ€ÐµÐ³Ð°Ñ‚Ð°
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
    public DateTime? LastLoginAtUtc { get; private set; }
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
    public ICollection<ShoppingList> ShoppingLists { get; private set; } = new List<ShoppingList>();
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    // ÐšÐ¾Ð½ÑÑ‚Ñ€ÑƒÐºÑ‚Ð¾Ñ€ Ð´Ð»Ñ EF Core
    private User() {
    }

    // Factory method Ð´Ð»Ñ ÑÐ¾Ð·Ð´Ð°Ð½Ð¸Ñ Ð½Ð¾Ð²Ð¾Ð³Ð¾ Ð¿Ð¾Ð»ÑŒÐ·Ð¾Ð²Ð°Ñ‚ÐµÐ»Ñ
    public static User Create(string email, string hashedPassword) {
        var normalizedEmail = NormalizeRequiredEmail(email);
        var normalizedPassword = NormalizeRequiredPasswordHash(hashedPassword);

        var user = new User {
            Id = UserId.New(),
            Email = normalizedEmail,
            Password = normalizedPassword,
            AiInputTokenLimit = DefaultAiInputTokenLimit,
            AiOutputTokenLimit = DefaultAiOutputTokenLimit,
            IsEmailConfirmed = false
        };
        user.SetCreated();
        return user;
    }

    public void UpdateRefreshToken(string? refreshToken) {
        RefreshToken = refreshToken;
        LastLoginAtUtc = DateTime.UtcNow;
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
        Password = NormalizeRequiredPasswordHash(hashedPassword);
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
            if (inputLimit.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputLimit), "Input limit must be non-negative.");
            }

            AiInputTokenLimit = inputLimit.Value;
        }

        if (outputLimit.HasValue)
        {
            if (outputLimit.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outputLimit), "Output limit must be non-negative.");
            }

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
        if (DeletedAt is not null && !IsActive)
        {
            return;
        }

        DeletedAt = deletedAtUtc;
        IsActive = false;
        RaiseDomainEvent(new UserDeletedDomainEvent(Id, deletedAtUtc));
        SetModified();
    }

    public void Restore()
    {
        if (DeletedAt is null && IsActive)
        {
            return;
        }

        DeletedAt = null;
        IsActive = true;
        RaiseDomainEvent(new UserRestoredDomainEvent(Id));
        SetModified();
    }

    private static string NormalizeRequiredEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email is required.", nameof(value));
        }

        return value.Trim();
    }

    private static string NormalizeRequiredPasswordHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Password hash is required.", nameof(value));
        }

        return value;
    }
}

