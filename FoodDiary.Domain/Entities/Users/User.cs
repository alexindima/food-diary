using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using DesiredWaistValueObject = FoodDiary.Domain.ValueObjects.DesiredWaist;
using DesiredWeightValueObject = FoodDiary.Domain.ValueObjects.DesiredWeight;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User : AggregateRoot<UserId> {
    private const long DefaultAiInputTokenLimit = 5_000_000;
    private const long DefaultAiOutputTokenLimit = 1_000_000;
    private const double ComparisonEpsilon = 0.000001d;

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

    private readonly List<Meal> _meals = [];
    private readonly List<Product> _products = [];
    private readonly List<Recipe> _recipes = [];
    private readonly List<WeightEntry> _weightEntries = [];
    private readonly List<WaistEntry> _waistEntries = [];
    private readonly List<Cycle> _cycles = [];
    private readonly List<HydrationEntry> _hydrationEntries = [];
    private readonly List<ShoppingList> _shoppingLists = [];
    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<Meal> Meals => _meals.AsReadOnly();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();
    public IReadOnlyCollection<Recipe> Recipes => _recipes.AsReadOnly();
    public IReadOnlyCollection<WeightEntry> WeightEntries => _weightEntries.AsReadOnly();
    public IReadOnlyCollection<WaistEntry> WaistEntries => _waistEntries.AsReadOnly();
    public IReadOnlyCollection<Cycle> Cycles => _cycles.AsReadOnly();
    public IReadOnlyCollection<HydrationEntry> HydrationEntries => _hydrationEntries.AsReadOnly();
    public IReadOnlyCollection<ShoppingList> ShoppingLists => _shoppingLists.AsReadOnly();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private User() {
    }

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
        user.ApplyCredentialState(UserCredentialState.CreateInitial());
        user.ApplyProfileState(UserProfileState.CreateInitial());
        user.ApplyGoalState(UserGoalState.CreateInitial());
        user.SetCreated();
        return user;
    }

    private static string NormalizeRequiredEmail(string value) {
        return EmailAddress.Create(value).Value;
    }

    private static string NormalizeRequiredPasswordHash(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Password hash is required.", nameof(value));
        }

        return value;
    }

    private static string? NormalizeOptionalToken(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return value.Trim();
    }

    private static string NormalizeRequiredTokenHash(string value, string paramName) {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Token hash is required.", paramName)
            : value.Trim();
    }

    private static DateTime NormalizeUtcTimestamp(DateTime value, string paramName) {
        if (value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.");
        }

        return value.ToUniversalTime();
    }

    private static DateTime NormalizeOptionalAuditTimestamp(DateTime? value, string paramName) {
        return value.HasValue
            ? NormalizeUtcTimestamp(value.Value, paramName)
            : DomainTime.UtcNow;
    }

    private static void EnsureFutureUtc(DateTime value, string paramName) {
        if (value <= DomainTime.UtcNow) {
            throw new ArgumentOutOfRangeException(paramName, "Date must be in the future (UTC).");
        }
    }

    private static void EnsureBirthDateIsNotFuture(DateTime? birthDate) {
        if (birthDate.HasValue && birthDate.Value.Date > DomainTime.UtcNow.Date) {
            throw new ArgumentOutOfRangeException(nameof(birthDate), "Birth date cannot be in the future.");
        }
    }

    private static void EnsurePositive(double? value, string paramName) {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value))) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be a finite number.");
        }

        if (value is <= 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be positive.");
        }
    }

    private void EnsureNotDeleted() {
        if (DeletedAt is not null) {
            throw new InvalidOperationException("Deleted user cannot be mutated. Use Restore() first.");
        }
    }

    private UserNutritionGoals GetNutritionGoals() {
        return UserNutritionGoals.Create(
            DailyCalorieTarget,
            ProteinTarget,
            FatTarget,
            CarbTarget,
            FiberTarget,
            WaterGoal);
    }

    private UserGoalState GetGoalState() {
        return new UserGoalState(
            DailyCalorieTarget,
            ProteinTarget,
            FatTarget,
            CarbTarget,
            FiberTarget,
            WaterGoal,
            DesiredWeight,
            DesiredWaist);
    }

    private void ApplyGoalState(UserGoalState state) {
        DailyCalorieTarget = state.DailyCalorieTarget;
        ProteinTarget = state.ProteinTarget;
        FatTarget = state.FatTarget;
        CarbTarget = state.CarbTarget;
        FiberTarget = state.FiberTarget;
        WaterGoal = state.WaterGoal;
        DesiredWeight = state.DesiredWeight;
        DesiredWaist = state.DesiredWaist;
    }

    private UserActivityGoals GetActivityGoals() {
        return UserActivityGoals.Create(StepGoal, HydrationGoal);
    }

    private void ApplyActivityGoals(UserActivityGoals goals) {
        StepGoal = goals.StepGoal;
        HydrationGoal = goals.HydrationGoal;
    }

    private UserProfileState GetProfileState() {
        return new UserProfileState(
            Username,
            FirstName,
            LastName,
            BirthDate,
            Gender,
            Weight,
            Height,
            ActivityLevel,
            ProfileImage,
            ProfileImageAssetId,
            DashboardLayoutJson,
            Language);
    }

    private void ApplyProfileState(UserProfileState state) {
        Username = state.Username;
        FirstName = state.FirstName;
        LastName = state.LastName;
        BirthDate = state.BirthDate;
        Gender = state.Gender;
        Weight = state.Weight;
        Height = state.Height;
        ActivityLevel = state.ActivityLevel;
        ProfileImage = state.ProfileImage;
        ProfileImageAssetId = state.ProfileImageAssetId;
        DashboardLayoutJson = state.DashboardLayoutJson;
        Language = state.Language;
    }

    private UserCredentialState GetCredentialState() {
        return new UserCredentialState(
            RefreshToken,
            IsEmailConfirmed,
            EmailConfirmationTokenHash,
            EmailConfirmationTokenExpiresAtUtc,
            EmailConfirmationSentAtUtc,
            PasswordResetTokenHash,
            PasswordResetTokenExpiresAtUtc,
            PasswordResetSentAtUtc,
            LastLoginAtUtc);
    }

    private void ApplyCredentialState(UserCredentialState state) {
        RefreshToken = state.RefreshToken;
        IsEmailConfirmed = state.IsEmailConfirmed;
        EmailConfirmationTokenHash = state.EmailConfirmationTokenHash;
        EmailConfirmationTokenExpiresAtUtc = state.EmailConfirmationTokenExpiresAtUtc;
        EmailConfirmationSentAtUtc = state.EmailConfirmationSentAtUtc;
        PasswordResetTokenHash = state.PasswordResetTokenHash;
        PasswordResetTokenExpiresAtUtc = state.PasswordResetTokenExpiresAtUtc;
        PasswordResetSentAtUtc = state.PasswordResetSentAtUtc;
        LastLoginAtUtc = state.LastLoginAtUtc;
    }

    private static void EnsureLanguage(string? value, string paramName) {
        if (value is null) {
            return;
        }

        if (!LanguageCode.TryParse(value, out _)) {
            throw new ArgumentOutOfRangeException(paramName, "Language must be one of the supported codes.");
        }
    }

    private static void EnsureDesiredWeight(double? value, string paramName) {
        if (!value.HasValue) {
            return;
        }

        try {
            _ = DesiredWeightValueObject.Create(value.Value);
        } catch (ArgumentOutOfRangeException ex) {
            throw new ArgumentOutOfRangeException(paramName, ex.Message);
        }
    }

    private static void EnsureDesiredWaist(double? value, string paramName) {
        if (!value.HasValue) {
            return;
        }

        try {
            _ = DesiredWaistValueObject.Create(value.Value);
        } catch (ArgumentOutOfRangeException ex) {
            throw new ArgumentOutOfRangeException(paramName, ex.Message);
        }
    }

    private static void EnsureGender(string? value, string paramName) {
        if (value is null) {
            return;
        }

        if (!GenderCode.TryParse(value, out _)) {
            throw new ArgumentOutOfRangeException(paramName, "Gender must be one of the supported codes.");
        }
    }

    private static string NormalizeRequiredGender(string value, string paramName) {
        if (!GenderCode.TryParse(value, out var gender)) {
            throw new ArgumentOutOfRangeException(paramName, "Gender must be one of the supported codes.");
        }

        return gender.Value;
    }

    private static string NormalizeOptionalLanguage(string? value, string paramName) {
        if (value is null) {
            return string.Empty;
        }

        return !LanguageCode.TryParse(value, out var languageCode)
            ? throw new ArgumentOutOfRangeException(paramName, "Language must be one of the supported codes.")
            : languageCode.Value;
    }

    private static string? NormalizeOptionalProfileText(string? value) {
        return value?.Trim();
    }

    private static bool NullableAreClose(double? left, double? right) {
        if (!left.HasValue && !right.HasValue) {
            return true;
        }

        if (!left.HasValue || !right.HasValue) {
            return false;
        }

        return Math.Abs(left.Value - right.Value) <= ComparisonEpsilon;
    }

    private static bool NullableAreClose(double? left, double right) {
        return left.HasValue && Math.Abs(left.Value - right) <= ComparisonEpsilon;
    }

    public void ReplaceRoles(IReadOnlyCollection<Role> roles) {
        ArgumentNullException.ThrowIfNull(roles);
        EnsureNotDeleted();

        var requestedRoleIds = roles
            .Select(role => role.Id)
            .OrderBy(id => id.Value)
            .ToArray();
        var currentRoleIds = _userRoles
            .Select(role => role.RoleId)
            .OrderBy(id => id.Value)
            .ToArray();

        if (requestedRoleIds.SequenceEqual(currentRoleIds)) {
            return;
        }

        var nextRoles = roles
            .Select(role => UserRole.Create(this, role))
            .ToList();

        _userRoles.Clear();
        _userRoles.AddRange(nextRoles);

        SetModified();
    }
}
