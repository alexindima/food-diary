using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class UserDataLifecycleGuardrailTests {
    private static readonly IReadOnlyDictionary<string, UserDataLifecyclePolicy> ClassifiedRelationships =
        CreateClassifications();

    [Fact]
    public void EveryDirectUserForeignKey_HasAnExplicitLifecycleClassification() {
        string[] actualRelationships = GetDirectUserRelationships();
        string[] missing = [.. actualRelationships.Except(ClassifiedRelationships.Keys, StringComparer.Ordinal)];
        string[] stale = [.. ClassifiedRelationships.Keys.Except(actualRelationships, StringComparer.Ordinal)];

        Assert.True(
            missing.Length == 0 && stale.Length == 0,
            $"Every direct User foreign key must be classified as delete, cascade, anonymize, retain, or reassign.{Environment.NewLine}" +
            $"Missing:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}{Environment.NewLine}" +
            $"Stale:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}");
    }

    [Fact]
    public void RetainedOrReassignedUserData_HasDocumentedRationale() {
        string[] violations = [.. ClassifiedRelationships
            .Where(static entry => entry.Value.Disposition is UserDataDisposition.Retain or UserDataDisposition.Reassign or UserDataDisposition.Anonymize)
            .Where(static entry => string.IsNullOrWhiteSpace(entry.Value.Rationale))
            .Select(static entry => entry.Key)
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void RestrictedUserRelationships_AreExplicitlyHandledByCleanupService() {
        string cleanupSource = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Infrastructure",
            "Persistence",
            "Users",
            "UserCleanupService.cs"));
        string[] requiredCleanupTargets = [
            "dbContext.AdminImpersonationSessions",
            "dbContext.ClientTasks",
        ];

        string[] violations = [.. requiredCleanupTargets
            .Where(target => !cleanupSource.Contains(target, StringComparison.Ordinal))];

        Assert.Empty(violations);
    }

    private static IReadOnlyDictionary<string, UserDataLifecyclePolicy> CreateClassifications() {
        string[] cascadeRelationships = [
            "AiQuotaPeriod(UserId):Cascade",
            "AiUsage(UserId):Cascade",
            "BillingPayment(UserId):Cascade",
            "BillingSubscription(UserId):Cascade",
            "ContentReport(UserId):Cascade",
            "CycleProfile(UserId):Cascade",
            "DietologistInvitation(ClientUserId):Cascade",
            "ExerciseEntry(UserId):Cascade",
            "FastingCheckIn(UserId):Cascade",
            "FastingOccurrence(UserId):Cascade",
            "FastingPlan(UserId):Cascade",
            "FastingSession(UserId):Cascade",
            "FavoriteMeal(UserId):Cascade",
            "FavoriteProduct(UserId):Cascade",
            "FavoriteRecipe(UserId):Cascade",
            "HydrationEntry(UserId):Cascade",
            "ImageAsset(UserId):Cascade",
            "Meal(UserId):Cascade",
            "MealPlan(UserId):Cascade",
            "Notification(UserId):Cascade",
            "RecentItem(UserId):Cascade",
            "RecipeComment(UserId):Cascade",
            "RecipeLike(UserId):Cascade",
            "Recommendation(ClientUserId):Cascade",
            "Recommendation(DietologistUserId):Cascade",
            "RecommendationBulkDispatch(ClientUserId):Cascade",
            "RecommendationBulkDispatch(DietologistUserId):Cascade",
            "RecommendationComment(AuthorUserId):Cascade",
            "RecommendationTemplate(DietologistUserId):Cascade",
            "ShoppingList(UserId):Cascade",
            "UserAchievement(UserId):Cascade",
            "UserLessonProgress(UserId):Cascade",
            "UserLoginEvent(UserId):Cascade",
            "UserRefreshTokenSession(UserId):Cascade",
            "UserRole(UserId):Cascade",
            "UserRoleAuditEvent(UserId):Cascade",
            "WaistEntry(UserId):Cascade",
            "WaistGoal(UserId):Cascade",
            "WearableConnection(UserId):Cascade",
            "WearableSyncEntry(UserId):Cascade",
            "WebPushSubscription(UserId):Cascade",
            "WeeklyGoal(UserId):Cascade",
            "WeightEntry(UserId):Cascade",
            "WeightGoal(UserId):Cascade",
        ];
        Dictionary<string, UserDataLifecyclePolicy> policies = cascadeRelationships.ToDictionary(
            static relationship => relationship,
            static _ => new UserDataLifecyclePolicy(UserDataDisposition.Cascade, string.Empty),
            StringComparer.Ordinal);
        policies.Add("AdminImpersonationSession(ActorUserId):Restrict", new UserDataLifecyclePolicy(UserDataDisposition.Delete, "Impersonation sessions are security credentials and are deleted before the user row."));
        policies.Add("AdminImpersonationSession(TargetUserId):Restrict", new UserDataLifecyclePolicy(UserDataDisposition.Delete, "Impersonation sessions are security credentials and are deleted before the user row."));
        policies.Add("ClientTask(ClientUserId):Restrict", new UserDataLifecyclePolicy(UserDataDisposition.Delete, "Client collaboration tasks are removed when either participant is deleted."));
        policies.Add("ClientTask(DietologistUserId):Restrict", new UserDataLifecyclePolicy(UserDataDisposition.Delete, "Client collaboration tasks are removed when either participant is deleted."));
        policies.Add("DietologistInvitation(DietologistUserId):SetNull", new UserDataLifecyclePolicy(UserDataDisposition.Anonymize, "The optional dietologist reference is cleared by the database."));
        policies.Add("UserRoleAuditEvent(ActorUserId):SetNull", new UserDataLifecyclePolicy(UserDataDisposition.Anonymize, "The optional actor reference is cleared while the role audit event follows its configured retention."));
        policies.Add("Product(UserId):Cascade", new UserDataLifecyclePolicy(UserDataDisposition.Reassign, "User-owned catalog content may be reassigned to the configured active cleanup owner; otherwise it is deleted."));
        policies.Add("Recipe(UserId):Cascade", new UserDataLifecyclePolicy(UserDataDisposition.Reassign, "User-owned recipe content may be reassigned to the configured active cleanup owner; otherwise it is deleted."));
        return policies;
    }

    private static string[] GetDirectUserRelationships() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql("Host=localhost;Database=food_diary_architecture;Username=test;Password=test")
            .Options;
        using var context = new FoodDiaryDbContext(options);

        return [.. context.Model.GetEntityTypes()
            .SelectMany(static entity => entity.GetForeignKeys())
            .Where(static foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(User))
            .Select(static foreignKey => $"{foreignKey.DeclaringEntityType.ClrType.Name}({string.Join(',', foreignKey.Properties.Select(static property => property.Name))}):{foreignKey.DeleteBehavior}")
            .Order(StringComparer.Ordinal)];
    }

    private enum UserDataDisposition {
        Delete = 0,
        Cascade = 1,
        Anonymize = 2,
        Retain = 3,
        Reassign = 4,
    }

    [ExcludeFromCodeCoverage]
    private sealed record UserDataLifecyclePolicy(
        UserDataDisposition Disposition,
        string Rationale);
}
