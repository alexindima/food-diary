using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationErrorCatalogTests {
    private static readonly TimeSpan ErrorCodeRegexTimeout = TimeSpan.FromSeconds(1);

    [Fact]
    public void ApplicationLayer_UsesSharedAndModuleErrorCatalog_ExceptValidationBehavior() {
        string applicationRoot = ResolveApplicationRoot();
        Assert.True(Directory.Exists(applicationRoot), $"Missing application runtime source: {applicationRoot}");
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            Path.Combine(applicationRoot, "Common", "Behaviors", "ValidationBehavior.cs"),
        };

        string[] violations = [.. Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !allowedFiles.Contains(path))
            .Where(ContainsAdHocErrorConstruction)
            .Select(path => Path.GetRelativePath(applicationRoot, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void SharedAndModuleErrorCatalog_DefinesErrorKind_ForAllPublishedErrors() {
        string[] missingKinds = [.. typeof(Errors)
            .GetNestedTypes(BindingFlags.Public)
            .Concat([
                typeof(FoodDiary.Modules.Ai.Application.Abstractions.Common.AiErrors),
                typeof(FoodDiary.Modules.Billing.Application.Abstractions.Common.BillingErrors),
                typeof(FoodDiary.Modules.Cycles.Contracts.Common.CycleErrors),
                typeof(FoodDiary.Modules.Dietologist.Application.Abstractions.Common.DietologistErrors),
                typeof(FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common.FavoriteMealErrors),
                typeof(FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common.FavoriteProductErrors),
                typeof(FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common.FavoriteRecipeErrors),
                typeof(FoodDiary.Modules.Images.Application.Abstractions.Common.ImageErrors),
                typeof(FoodDiary.Modules.Lessons.Application.Abstractions.Common.LessonErrors),
                typeof(FoodDiary.Modules.Admin.Application.Abstractions.Common.AdminMailInboxErrors),
                typeof(FoodDiary.Modules.Meals.Application.Abstractions.Common.MealErrors),
                typeof(FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common.MealPlanErrors),
                typeof(FoodDiary.Modules.Products.Contracts.Common.ProductErrors),
                typeof(FoodDiary.Modules.Recipes.Contracts.Common.RecipeErrors),
                typeof(FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Common.RecipeCommentErrors),
                typeof(FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common.ShoppingListErrors),
                typeof(FoodDiary.Modules.Usda.Application.Abstractions.Common.UsdaErrors),
                typeof(FoodDiary.Modules.Users.Contracts.Common.UserErrors),
                typeof(FoodDiary.Modules.Wearables.Application.Abstractions.Common.WearableErrors),
            ])
            .SelectMany(GetErrorsFromType)
            .Where(static error => error.Kind is null)
            .Select(static error => error.Code)
            .Distinct(StringComparer.Ordinal)];

        Assert.Empty(missingKinds);
    }

    [Fact]
    public void ApplicationLayer_StringErrorCodes_UseKnownCatalogCodes() {
        string applicationRoot = ResolveApplicationRoot();
        Assert.True(Directory.Exists(applicationRoot), $"Missing application runtime source: {applicationRoot}");
        HashSet<string> knownCodes = GetKnownErrorCodes();

        string[] violations = [.. Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => GetReferencedStringErrorCodes(path)
                .Where(code => !knownCodes.Contains(code))
                .Select(code => $"{Path.GetRelativePath(applicationRoot, path)}: {code}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    private static string ResolveApplicationRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            if (File.Exists(Path.Combine(directory.FullName, "FoodDiary.slnx"))) {
                return Path.Combine(directory.FullName, "Shared", "FoodDiary.Application.Runtime");
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the repository root from '{AppContext.BaseDirectory}'.");
    }

    private static bool ContainsAdHocErrorConstruction(string path) {
        string content = File.ReadAllText(path);
        return content.Contains("new Error(", StringComparison.Ordinal) ||
               content.Contains("new Error (", StringComparison.Ordinal);
    }

    private static HashSet<string> GetKnownErrorCodes() {
        IEnumerable<string> publishedCodes = typeof(Errors)
            .GetNestedTypes(BindingFlags.Public)
            .Concat([
                typeof(FoodDiary.Modules.Ai.Application.Abstractions.Common.AiErrors),
                typeof(FoodDiary.Modules.Billing.Application.Abstractions.Common.BillingErrors),
                typeof(FoodDiary.Modules.Cycles.Contracts.Common.CycleErrors),
                typeof(FoodDiary.Modules.Dietologist.Application.Abstractions.Common.DietologistErrors),
                typeof(FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common.FavoriteMealErrors),
                typeof(FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common.FavoriteProductErrors),
                typeof(FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common.FavoriteRecipeErrors),
                typeof(FoodDiary.Modules.Images.Application.Abstractions.Common.ImageErrors),
                typeof(FoodDiary.Modules.Lessons.Application.Abstractions.Common.LessonErrors),
                typeof(FoodDiary.Modules.Admin.Application.Abstractions.Common.AdminMailInboxErrors),
                typeof(FoodDiary.Modules.Meals.Application.Abstractions.Common.MealErrors),
                typeof(FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common.MealPlanErrors),
                typeof(FoodDiary.Modules.Products.Contracts.Common.ProductErrors),
                typeof(FoodDiary.Modules.Recipes.Contracts.Common.RecipeErrors),
                typeof(FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Common.RecipeCommentErrors),
                typeof(FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common.ShoppingListErrors),
                typeof(FoodDiary.Modules.Usda.Application.Abstractions.Common.UsdaErrors),
                typeof(FoodDiary.Modules.Users.Contracts.Common.UserErrors),
                typeof(FoodDiary.Modules.Wearables.Application.Abstractions.Common.WearableErrors),
            ])
            .SelectMany(GetErrorsFromType)
            .Select(static error => error.Code);

        IEnumerable<string> resolverCodes = typeof(ErrorKindResolver)
            .GetField("ExactMappings", BindingFlags.NonPublic | BindingFlags.Static)?
            .GetValue(null) is IReadOnlyDictionary<string, ErrorKind> mappings
            ? mappings.Keys
            : [];

        return publishedCodes
            .Concat(resolverCodes)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IEnumerable<string> GetReferencedStringErrorCodes(string path) {
        string content = File.ReadAllText(path);
        MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(
            content,
            @"(?:WithErrorCode\(|ErrorCode\s*=\s*)""(?<code>[A-Za-z]+\.[A-Za-z]+)""",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant,
            ErrorCodeRegexTimeout);

        return matches
            .Select(match => match.Groups["code"].Value)
            .Distinct(StringComparer.Ordinal);
    }

    private static IEnumerable<Error> GetErrorsFromType(Type type) {
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Static)) {
            if (property.PropertyType != typeof(Error) || property.GetIndexParameters().Length > 0) {
                continue;
            }

            if (property.GetValue(null) is Error error) {
                yield return error;
            }
        }

        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if (method.ReturnType != typeof(Error) || method.IsSpecialName) {
                continue;
            }

            object?[] arguments = [.. method.GetParameters().Select(CreateSampleArgument)];

            if (method.Invoke(null, arguments) is Error error) {
                yield return error;
            }
        }
    }

    private static object? CreateSampleArgument(ParameterInfo parameter) {
        Type parameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

        if (parameterType == typeof(Guid)) {
            return Guid.Empty;
        }

        if (parameterType == typeof(int)) {
            return 0;
        }

        if (parameterType == typeof(DateTime)) {
            return DateTime.UnixEpoch;
        }

        if (parameterType == typeof(DateOnly)) {
            return DateOnly.FromDateTime(DateTime.UnixEpoch);
        }

        if (parameterType == typeof(string)) {
            return parameter.Name switch {
                "field" => "field",
                "reason" => "reason",
                "locale" => "en",
                _ => "sample",
            };
        }

        throw new InvalidOperationException($"Unsupported error catalog parameter type: {parameter.ParameterType.FullName}");
    }
}
