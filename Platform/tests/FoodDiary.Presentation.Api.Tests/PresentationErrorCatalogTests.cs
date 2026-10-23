using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Cycles.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class PresentationErrorCatalogTests {
    [Fact]
    public void SharedAndModuleErrorCatalog_HasExpectedHttpCoverage() {
        var internalOnlyCodes = new HashSet<string>(StringComparer.Ordinal) {
            "Authentication.GoogleNotConfigured",
            "Authentication.TelegramNotConfigured",
            "Authentication.TelegramBotNotConfigured",
            "Meal.InvalidData",
            "Product.InvalidData",
            "Recipe.InvalidData",
            "Wearable.ProviderNotConfigured",
        };

        foreach (Error error in GetCatalogErrors()) {
            int statusCode = PresentationErrorHttpMapper.MapStatusCode(error);

            if (internalOnlyCodes.Contains(error.Code)) {
                Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
                continue;
            }

            Assert.NotEqual(
                StatusCodes.Status500InternalServerError,
                statusCode);
        }
    }

    [Fact]
    public void SharedAndModuleErrorCatalog_DefinesErrorKind_ForAllPublishedErrors() {
        string[] missingKinds = [.. GetCatalogErrors()
            .Where(static error => error.Kind is null)
            .Select(static error => error.Code)
            .Distinct(StringComparer.Ordinal)];

        Assert.Empty(missingKinds);
    }

    [Fact]
    public void SharedAndModuleErrorCatalog_CanBeEnumeratedWithoutDuplicatesOrMissingCodes() {
        string[] duplicates = [.. GetCatalogErrors()
            .GroupBy(static error => error.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Where(group => group.Key is not "User.NotFound" and not "ShoppingList.NotFound")
            .Select(group => group.Key)];

        Assert.Empty(duplicates);
    }

    private static IReadOnlyList<Error> GetCatalogErrors() {
        return typeof(Errors)
            .GetNestedTypes(BindingFlags.Public)
            .Concat([
                typeof(AiErrors),
                typeof(BillingErrors),
                typeof(CycleErrors),
                typeof(DietologistErrors),
                typeof(FavoriteMealErrors),
                typeof(FavoriteProductErrors),
                typeof(FavoriteRecipeErrors),
                typeof(ImageErrors),
                typeof(LessonErrors),
                typeof(AdminMailInboxErrors),
                typeof(MealErrors),
                typeof(MealPlanErrors),
                typeof(ProductErrors),
                typeof(RecipeErrors),
                typeof(RecipeCommentErrors),
                typeof(ShoppingListErrors),
                typeof(UsdaErrors),
                typeof(UserErrors),
                typeof(WearableErrors),
            ])
            .SelectMany(GetErrorsFromType)
            .OrderBy(static error => error.Code, StringComparer.Ordinal)
            .ToArray();
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
            if (method.ReturnType != typeof(Error)) {
                continue;
            }

            if (method.IsSpecialName) {
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
