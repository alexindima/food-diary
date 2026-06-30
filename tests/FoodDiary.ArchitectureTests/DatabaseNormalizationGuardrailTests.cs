using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DatabaseNormalizationGuardrailTests {
    private static readonly HashSet<string> AllowedDocumentColumns = new(StringComparer.Ordinal) {
        "BillingPayment.ProviderMetadataJson",
        "BillingSubscription.ProviderMetadataJson",
        "BillingWebhookEvent.PayloadJson",
        // User-entered symptom tags are small per-entry annotations, not shared facts or reporting dimensions.
        "CycleSymptomEntry.TagsJson",
        "Notification.PayloadJson",
        "User.DashboardLayoutJson",
    };

    private static readonly HashSet<string> AllowedSnapshotColumns = new(StringComparer.Ordinal) {
        "FavoriteMeal.Name",
        "FavoriteProduct.Name",
        "FavoriteRecipe.Name",
        "ShoppingListItem.Category",
        "ShoppingListItem.Name",
        "UserRoleAuditEvent.RoleName",
    };

    private static readonly HashSet<string> AllowedDerivedColumns = new(StringComparer.Ordinal) {
        "AiUsage.TotalTokens",
        "Meal.ManualAlcohol",
        "Meal.ManualCalories",
        "Meal.ManualCarbs",
        "Meal.ManualFats",
        "Meal.ManualFiber",
        "Meal.ManualProteins",
        "Meal.TotalAlcohol",
        "Meal.TotalCalories",
        "Meal.TotalCarbs",
        "Meal.TotalFats",
        "Meal.TotalFiber",
        "Meal.TotalProteins",
        "Recipe.ManualAlcohol",
        "Recipe.ManualCalories",
        "Recipe.ManualCarbs",
        "Recipe.ManualFats",
        "Recipe.ManualFiber",
        "Recipe.ManualProteins",
        "Recipe.TotalAlcohol",
        "Recipe.TotalCalories",
        "Recipe.TotalCarbs",
        "Recipe.TotalFats",
        "Recipe.TotalFiber",
        "Recipe.TotalProteins",
    };

    private static readonly HashSet<string> AllowedRawSqlDocumentColumns = new(StringComparer.Ordinal) {
        "MailInbox/FoodDiary.MailInbox.Infrastructure/Services/NpgsqlInboundMailStore.cs:to_recipients_json",
        "MailRelay/FoodDiary.MailRelay.Infrastructure/Services/MailRelayQueueSchema.cs:to_recipients_json",
    };

    private static readonly BusinessKeyExpectation[] ExpectedBusinessKeys = [
        new("User", ["Email"]),
        new("User", ["TelegramUserId"]),
        new("Role", ["Name"]),
        new("UserRole", ["UserId", "RoleId"]),
        new("WeightEntry", ["UserId", "Date"]),
        new("WaistEntry", ["UserId", "Date"]),
        new("RecentItem", ["UserId", "ItemType", "ItemId"]),
        new("RecipeLike", ["UserId", "RecipeId"]),
        new("UserLessonProgress", ["UserId", "LessonId"]),
        new("FavoriteMeal", ["UserId", "MealId"]),
        new("FavoriteProduct", ["UserId", "ProductId"]),
        new("FavoriteRecipe", ["UserId", "RecipeId"]),
        new("WearableConnection", ["UserId", "Provider"]),
        new("WearableSyncEntry", ["UserId", "Provider", "DataType", "Date"]),
        new("BillingSubscription", ["UserId"]),
        new("BillingSubscription", ["Provider", "ExternalCustomerId"]),
        new("BillingSubscription", ["Provider", "ExternalSubscriptionId"]),
        new("BillingPayment", ["Provider", "ExternalPaymentId"]),
        new("BillingWebhookEvent", ["Provider", "EventId"]),
        new("EmailTemplate", ["Key", "Locale"]),
        new("AiPromptTemplate", ["Key", "Locale"]),
        new("MealPlanDay", ["MealPlanId", "DayNumber"]),
        new("FastingOccurrence", ["PlanId", "SequenceNumber"]),
        new("UsdaFoodNutrient", ["FdcId", "NutrientId"]),
        new("DailyReferenceValue", ["NutrientId", "AgeGroup", "Gender"]),
    ];

    [Fact]
    public void FirstNormalForm_DocumentColumnsRemainExplicitlyApproved() {
        string[] violations = [.. RelationalEntityTypes()
            .SelectMany(entity => entity.GetProperties()
                .Where(IsPersistedApplicationColumn)
                .Where(IsDocumentShapedColumn)
                .Select(property => FormatProperty(entity, property)))
            .Where(column => !AllowedDocumentColumns.Contains(column))
            .OrderBy(static column => column, StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "New document-shaped columns weaken the relational 1NF guardrail. Normalize the data, or add an explicit exception with a product reason. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void FirstNormalForm_RawSqlSchemasDoNotIntroduceUnapprovedJsonColumns() {
        string[] violations = [.. SourceScanner.SourceFiles([
                ArchitectureTestPaths.FromRoot("MailInbox/FoodDiary.MailInbox.Infrastructure"),
                ArchitectureTestPaths.FromRoot("MailRelay/FoodDiary.MailRelay.Infrastructure"),
            ])
            .SelectMany(ReadRawSqlJsonColumns)
            .Where(column => !AllowedRawSqlDocumentColumns.Contains(column))
            .OrderBy(static column => column, StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "Raw SQL schemas should not add JSON columns without an explicit normalization exception. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void SecondNormalForm_CompositePrimaryKeyTablesDoNotCarryNonKeyFacts() {
        string[] violations = [.. RelationalEntityTypes()
            .Where(static entity => entity.FindPrimaryKey()?.Properties.Count > 1)
            .SelectMany(entity => {
                var keyProperties = entity.FindPrimaryKey()!.Properties.ToHashSet();

                return entity.GetProperties()
                    .Where(property => !keyProperties.Contains(property))
                    .Where(IsPersistedApplicationColumn)
                    .Select(property => FormatProperty(entity, property));
            })
            .OrderBy(static column => column, StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "Composite-key link tables should not carry non-key facts unless the dependency is intentionally modeled elsewhere. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ThirdNormalForm_SnapshotColumnsRemainExplicitlyApproved() {
        string[] violations = [.. RelationalEntityTypes()
            .SelectMany(entity => FindSnapshotLikeColumns(entity)
                .Select(property => FormatProperty(entity, property)))
            .Where(column => !AllowedSnapshotColumns.Contains(column))
            .OrderBy(static column => column, StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "Columns that duplicate facts reachable through a foreign key should be deliberate snapshots/audit data. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ThirdNormalForm_DerivedColumnsRemainExplicitlyApproved() {
        string[] violations = [.. RelationalEntityTypes()
            .SelectMany(entity => entity.GetProperties()
                .Where(IsPersistedApplicationColumn)
                .Where(IsDerivedColumn)
                .Select(property => FormatProperty(entity, property)))
            .Where(column => !AllowedDerivedColumns.Contains(column))
            .OrderBy(static column => column, StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "Columns that cache derived values should be deliberate performance/product denormalizations. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void BcnfStyle_BusinessKeysStayUnique() {
        var entityTypes = RelationalEntityTypes()
            .ToDictionary(static entity => entity.ClrType.Name, StringComparer.Ordinal);

        string[] violations = [.. ExpectedBusinessKeys
            .Where(expectation => !HasUniqueKey(entityTypes, expectation))
            .Select(expectation => $"{expectation.EntityName}({string.Join(", ", expectation.PropertyNames)})")
            .OrderBy(static value => value, StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "Expected business determinants should stay backed by primary keys or unique indexes. Missing unique keys:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    private static IReadOnlyList<IEntityType> RelationalEntityTypes() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql("Host=localhost;Database=food_diary_architecture;Username=test;Password=test")
            .Options;

        using var context = new FoodDiaryDbContext(options);

        return context.Model.GetEntityTypes()
            .Where(static entity => entity.ClrType.Namespace?.StartsWith("FoodDiary.Domain.Entities", StringComparison.Ordinal) == true)
            .Where(static entity => !entity.IsOwned())
            .OrderBy(static entity => entity.ClrType.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsPersistedApplicationColumn(IProperty property) =>
        !property.IsShadowProperty() &&
        !property.IsIndexerProperty() &&
        !property.Name.Equals("xmin", StringComparison.Ordinal) &&
        property.DeclaringType is IEntityType declaringEntity &&
        property.GetColumnName(StoreObjectIdentifier.Table(declaringEntity.GetTableName()!, declaringEntity.GetSchema())) is not null;

    private static bool IsDocumentShapedColumn(IProperty property) {
        string columnType = property.GetColumnType();
        if (columnType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true) {
            return true;
        }

        return property.Name.EndsWith("Json", StringComparison.Ordinal) ||
               property.Name.EndsWith("Jsonb", StringComparison.Ordinal) ||
               property.Name.EndsWith("Csv", StringComparison.Ordinal);
    }

    private static bool IsDerivedColumn(IProperty property) =>
        property.Name.StartsWith("Total", StringComparison.Ordinal) ||
        property.Name.StartsWith("Manual", StringComparison.Ordinal);

    private static IEnumerable<IProperty> FindSnapshotLikeColumns(IEntityType entity) {
        var foreignKeyStems = entity.GetForeignKeys()
            .SelectMany(static key => key.Properties)
            .Select(static property => property.Name)
            .Where(static name => name.EndsWith("Id", StringComparison.Ordinal))
            .Select(static name => name[..^2])
            .ToHashSet(StringComparer.Ordinal);

        foreach (IProperty? property in entity.GetProperties().Where(IsPersistedApplicationColumn)) {
            if (property.ClrType != typeof(string)) {
                continue;
            }

            if (foreignKeyStems.Any(stem => property.Name.Equals($"{stem}Name", StringComparison.Ordinal))) {
                yield return property;
                continue;
            }

            if (property.Name is "Name" or "Category" &&
                foreignKeyStems.Overlaps(["Product", "Recipe", "Meal"])) {
                yield return property;
            }
        }
    }

    private static bool HasUniqueKey(
        IReadOnlyDictionary<string, IEntityType> entityTypes,
        BusinessKeyExpectation expectation) {
        if (!entityTypes.TryGetValue(expectation.EntityName, out IEntityType? entity)) {
            return false;
        }

        string[] expected = [.. expectation.PropertyNames.OrderBy(static name => name, StringComparer.Ordinal)];

        IKey? primaryKey = entity.FindPrimaryKey();
        if (primaryKey is not null && PropertyNamesMatch(primaryKey.Properties, expected)) {
            return true;
        }

        return entity.GetIndexes()
            .Any(index => index.IsUnique && PropertyNamesMatch(index.Properties, expected));
    }

    private static bool PropertyNamesMatch(IReadOnlyList<IProperty> properties, string[] expected) {
        string[] actual = [.. properties
            .Select(static property => property.Name)
            .OrderBy(static name => name, StringComparer.Ordinal)];

        return actual.SequenceEqual(expected, StringComparer.Ordinal);
    }

    private static IEnumerable<string> ReadRawSqlJsonColumns(string path) {
        string relativePath = Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path)
            .Replace(Path.DirectorySeparatorChar, '/');

        foreach (string line in File.ReadLines(path)) {
            string normalized = line.Trim();
            if (!normalized.Contains(" jsonb", StringComparison.OrdinalIgnoreCase) &&
                !normalized.Contains(" json ", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            string? columnName = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (columnName is null) {
                continue;
            }

            if (columnName.AsSpan().Contains('(') ||
                columnName.StartsWith('@') ||
                columnName.Equals("cast", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            yield return $"{relativePath}:{columnName}";
        }
    }

    private static string FormatProperty(IEntityType entity, IProperty property) =>
        $"{entity.ClrType.Name}.{property.Name}";

    [ExcludeFromCodeCoverage]
    private sealed record BusinessKeyExpectation(string EntityName, string[] PropertyNames);
}
