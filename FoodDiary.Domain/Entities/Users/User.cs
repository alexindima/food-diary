using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using DesiredWaistValueObject = FoodDiary.Domain.ValueObjects.DesiredWaist;
using DesiredWeightValueObject = FoodDiary.Domain.ValueObjects.DesiredWeight;

namespace FoodDiary.Domain.Entities.Users;

public sealed class User : AggregateRoot<UserId> {
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

    public ICollection<Meal> Meals { get; private set; } = new List<Meal>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();
    public ICollection<Recipe> Recipes { get; private set; } = new List<Recipe>();
    public ICollection<WeightEntry> WeightEntries { get; private set; } = new List<WeightEntry>();
    public ICollection<WaistEntry> WaistEntries { get; private set; } = new List<WaistEntry>();
    public ICollection<Cycle> Cycles { get; private set; } = new List<Cycle>();
    public ICollection<HydrationEntry> HydrationEntries { get; private set; } = new List<HydrationEntry>();
    public ICollection<ShoppingList> ShoppingLists { get; private set; } = new List<ShoppingList>();
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

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

    public void UpdateRefreshToken(string? refreshToken) {
        var normalizedRefreshToken = NormalizeOptionalToken(refreshToken);
        var nextState = GetCredentialState().WithRefreshToken(normalizedRefreshToken, DateTime.UtcNow);
        ApplyCredentialState(nextState);

        SetModified();
    }

    public void LinkTelegram(long telegramUserId) {
        TelegramUserId = telegramUserId;
        SetModified();
    }

    public void UnlinkTelegram() {
        TelegramUserId = null;
        SetModified();
    }

    public void UpdatePassword(string hashedPassword) {
        Password = NormalizeRequiredPasswordHash(hashedPassword);
        SetModified();
    }

    public void SetEmailConfirmationToken(string tokenHash, DateTime expiresAtUtc) {
        var normalizedTokenHash = NormalizeRequiredTokenHash(tokenHash, nameof(tokenHash));
        EnsureFutureUtc(expiresAtUtc, nameof(expiresAtUtc));
        var nextState = GetCredentialState().WithEmailConfirmationToken(normalizedTokenHash, expiresAtUtc, DateTime.UtcNow);
        ApplyCredentialState(nextState);
        SetModified();
    }

    public void ConfirmEmail() {
        SetEmailConfirmed(true);
    }

    public void SetEmailConfirmed(bool isConfirmed) {
        ApplyCredentialState(GetCredentialState().AsEmailConfirmed(isConfirmed));
        SetModified();
    }

    public void SetPasswordResetToken(string tokenHash, DateTime expiresAtUtc) {
        var normalizedTokenHash = NormalizeRequiredTokenHash(tokenHash, nameof(tokenHash));
        EnsureFutureUtc(expiresAtUtc, nameof(expiresAtUtc));
        var nextState = GetCredentialState().WithPasswordResetToken(normalizedTokenHash, expiresAtUtc, DateTime.UtcNow);
        ApplyCredentialState(nextState);
        SetModified();
    }

    public void ClearPasswordResetToken() {
        ApplyCredentialState(GetCredentialState().WithoutPasswordResetToken());
        SetModified();
    }

    public void UpdateProfile(UserProfileUpdate update) {
        var changed = false;
        changed |= ApplyPersonalInfoChanges(
            update.Username,
            update.FirstName,
            update.LastName,
            update.BirthDate,
            update.Gender,
            update.Weight,
            update.Height);
        changed |= ApplyActivityChanges(update.ActivityLevel, update.StepGoal, update.HydrationGoal);
        changed |= ApplyMediaAndPreferencesChanges(
            update.ProfileImage,
            update.ProfileImageAssetId,
            update.DashboardLayoutJson,
            update.Language);

        if (changed) {
            SetModified();
        }
    }

    public void UpdatePersonalInfo(
        string? username = null,
        string? firstName = null,
        string? lastName = null,
        DateTime? birthDate = null,
        string? gender = null,
        double? weight = null,
        double? height = null) {
        if (ApplyPersonalInfoChanges(username, firstName, lastName, birthDate, gender, weight, height)) {
            SetModified();
        }
    }

    public void UpdateActivity(
        ActivityLevel? activityLevel = null,
        int? stepGoal = null,
        double? hydrationGoal = null) {
        if (ApplyActivityChanges(activityLevel, stepGoal, hydrationGoal)) {
            SetModified();
        }
    }

    public void UpdatePreferences(
        string? dashboardLayoutJson = null,
        string? language = null) {
        if (ApplyPreferencesChanges(dashboardLayoutJson, language)) {
            SetModified();
        }
    }

    public void UpdateProfileMedia(
        string? profileImage = null,
        ImageAssetId? profileImageAssetId = null) {
        if (ApplyProfileMediaChanges(profileImage, profileImageAssetId)) {
            SetModified();
        }
    }

    public void UpdateGoals(
        double? dailyCalorieTarget = null,
        double? proteinTarget = null,
        double? fatTarget = null,
        double? carbTarget = null,
        double? fiberTarget = null,
        double? waterGoal = null,
        double? desiredWeight = null,
        double? desiredWaist = null) {
        var updatedGoals = GetNutritionGoals().With(
            dailyCalorieTarget: dailyCalorieTarget,
            proteinTarget: proteinTarget,
            fatTarget: fatTarget,
            carbTarget: carbTarget,
            fiberTarget: fiberTarget,
            waterGoal: waterGoal);

        EnsureDesiredWeight(desiredWeight, nameof(desiredWeight));
        EnsureDesiredWaist(desiredWaist, nameof(desiredWaist));

        var state = GetGoalState() with {
            DailyCalorieTarget = updatedGoals.DailyCalorieTarget,
            ProteinTarget = updatedGoals.ProteinTarget,
            FatTarget = updatedGoals.FatTarget,
            CarbTarget = updatedGoals.CarbTarget,
            FiberTarget = updatedGoals.FiberTarget,
            WaterGoal = updatedGoals.WaterGoal,
            DesiredWeight = desiredWeight.HasValue ? desiredWeight : DesiredWeight,
            DesiredWaist = desiredWaist.HasValue ? desiredWaist : DesiredWaist
        };

        ApplyGoalState(state);

        SetModified();
    }

    public void UpdateAiTokenLimits(long? inputLimit, long? outputLimit) {
        if (inputLimit.HasValue) {
            if (inputLimit.Value < 0) {
                throw new ArgumentOutOfRangeException(nameof(inputLimit), "Input limit must be non-negative.");
            }

            AiInputTokenLimit = inputLimit.Value;
        }

        if (outputLimit.HasValue) {
            if (outputLimit.Value < 0) {
                throw new ArgumentOutOfRangeException(nameof(outputLimit), "Output limit must be non-negative.");
            }

            AiOutputTokenLimit = outputLimit.Value;
        }

        SetModified();
    }

    public void UpdateDesiredWeight(double? desiredWeight) {
        EnsureDesiredWeight(desiredWeight, nameof(desiredWeight));
        ApplyGoalState(GetGoalState() with { DesiredWeight = desiredWeight });
        SetModified();
    }

    public void UpdateDesiredWaist(double? desiredWaist) {
        EnsureDesiredWaist(desiredWaist, nameof(desiredWaist));
        ApplyGoalState(GetGoalState() with { DesiredWaist = desiredWaist });
        SetModified();
    }

    public void Deactivate() {
        IsActive = false;
        SetModified();
    }

    public void Activate() {
        if (DeletedAt is not null) {
            throw new InvalidOperationException("Deleted user cannot be activated directly. Use Restore().");
        }

        IsActive = true;
        SetModified();
    }

    public void MarkDeleted(DateTime deletedAtUtc) {
        if (DeletedAt is not null && !IsActive) {
            return;
        }

        DeletedAt = deletedAtUtc;
        IsActive = false;
        RaiseDomainEvent(new UserDeletedDomainEvent(Id, deletedAtUtc));
        SetModified();
    }

    public void Restore() {
        if (DeletedAt is null && IsActive) {
            return;
        }

        DeletedAt = null;
        IsActive = true;
        RaiseDomainEvent(new UserRestoredDomainEvent(Id));
        SetModified();
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

    private static void EnsureFutureUtc(DateTime value, string paramName) {
        if (value <= DateTime.UtcNow) {
            throw new ArgumentOutOfRangeException(paramName, "Date must be in the future (UTC).");
        }
    }

    private static void EnsureBirthDateIsNotFuture(DateTime? birthDate) {
        if (birthDate.HasValue && birthDate.Value.Date > DateTime.UtcNow.Date) {
            throw new ArgumentOutOfRangeException(nameof(birthDate), "Birth date cannot be in the future.");
        }
    }

    private static void EnsurePositive(double? value, string paramName) {
        if (value is <= 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be positive.");
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

    private void ApplyNutritionGoals(UserNutritionGoals goals) {
        DailyCalorieTarget = goals.DailyCalorieTarget;
        ProteinTarget = goals.ProteinTarget;
        FatTarget = goals.FatTarget;
        CarbTarget = goals.CarbTarget;
        FiberTarget = goals.FiberTarget;
        WaterGoal = goals.WaterGoal;
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

    private bool ApplyPersonalInfoChanges(
        string? username,
        string? firstName,
        string? lastName,
        DateTime? birthDate,
        string? gender,
        double? weight,
        double? height) {
        var normalizedUsername = NormalizeOptionalProfileText(username);
        var normalizedFirstName = NormalizeOptionalProfileText(firstName);
        var normalizedLastName = NormalizeOptionalProfileText(lastName);

        EnsureBirthDateIsNotFuture(birthDate);
        EnsurePositive(weight, nameof(weight));
        EnsurePositive(height, nameof(height));
        EnsureGender(gender, nameof(gender));

        var state = GetProfileState();
        var changed = false;

        if (username is not null && state.Username != normalizedUsername) {
            state = state with { Username = normalizedUsername };
            changed = true;
        }

        if (firstName is not null && state.FirstName != normalizedFirstName) {
            state = state with { FirstName = normalizedFirstName };
            changed = true;
        }

        if (lastName is not null && state.LastName != normalizedLastName) {
            state = state with { LastName = normalizedLastName };
            changed = true;
        }

        if (birthDate.HasValue && state.BirthDate != birthDate) {
            state = state with { BirthDate = birthDate };
            changed = true;
        }

        if (gender is not null) {
            var normalizedGender = NormalizeRequiredGender(gender, nameof(gender));
            if (state.Gender != normalizedGender) {
                state = state with { Gender = normalizedGender };
                changed = true;
            }
        }

        if (weight.HasValue && !NullableAreClose(state.Weight, weight.Value)) {
            state = state with { Weight = weight };
            changed = true;
        }

        if (height.HasValue && !NullableAreClose(state.Height, height.Value)) {
            state = state with { Height = height };
            changed = true;
        }

        if (changed) {
            ApplyProfileState(state);
        }

        return changed;
    }

    private bool ApplyActivityChanges(
        ActivityLevel? activityLevel,
        int? stepGoal,
        double? hydrationGoal) {
        var updatedActivityGoals = GetActivityGoals().With(
            stepGoal: stepGoal,
            hydrationGoal: hydrationGoal);
        var state = GetProfileState();

        var changed = false;

        if (activityLevel.HasValue && state.ActivityLevel != activityLevel.Value) {
            state = state with { ActivityLevel = activityLevel.Value };
            changed = true;
        }

        if (StepGoal != updatedActivityGoals.StepGoal || !NullableAreClose(HydrationGoal, updatedActivityGoals.HydrationGoal)) {
            ApplyActivityGoals(updatedActivityGoals);
            changed = true;
        }

        if (changed) {
            ApplyProfileState(state);
        }

        return changed;
    }

    private bool ApplyMediaAndPreferencesChanges(
        string? profileImage,
        ImageAssetId? profileImageAssetId,
        string? dashboardLayoutJson,
        string? language) {
        var changed = false;
        changed |= ApplyProfileMediaChanges(profileImage, profileImageAssetId);
        changed |= ApplyPreferencesChanges(dashboardLayoutJson, language);
        return changed;
    }

    private bool ApplyProfileMediaChanges(string? profileImage, ImageAssetId? profileImageAssetId) {
        var normalizedProfileImage = NormalizeOptionalProfileText(profileImage);
        var state = GetProfileState();
        var changed = false;

        if (profileImage is not null && state.ProfileImage != normalizedProfileImage) {
            state = state with { ProfileImage = normalizedProfileImage };
            changed = true;
        }

        if (profileImageAssetId.HasValue && state.ProfileImageAssetId != profileImageAssetId) {
            state = state with { ProfileImageAssetId = profileImageAssetId };
            changed = true;
        }

        if (changed) {
            ApplyProfileState(state);
        }

        return changed;
    }

    private bool ApplyPreferencesChanges(string? dashboardLayoutJson, string? language) {
        var normalizedDashboardLayoutJson = NormalizeOptionalProfileText(dashboardLayoutJson);
        var normalizedLanguage = NormalizeOptionalLanguage(language, nameof(language));
        var state = GetProfileState();

        EnsureLanguage(language, nameof(language));

        var changed = false;

        if (dashboardLayoutJson is not null && state.DashboardLayoutJson != normalizedDashboardLayoutJson) {
            state = state with { DashboardLayoutJson = normalizedDashboardLayoutJson };
            changed = true;
        }

        if (language is not null && state.Language != normalizedLanguage) {
            state = state with { Language = normalizedLanguage };
            changed = true;
        }

        if (changed) {
            ApplyProfileState(state);
        }

        return changed;
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
}
