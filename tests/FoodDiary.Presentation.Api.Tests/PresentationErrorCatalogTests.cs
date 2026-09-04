using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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
                typeof(FoodDiary.Application.Abstractions.Ai.Common.AiErrors),
                typeof(FoodDiary.Application.Abstractions.Billing.Common.BillingErrors),
                typeof(FoodDiary.Application.Abstractions.Cycles.Common.CycleErrors),
                typeof(FoodDiary.Application.Abstractions.Cycles.Common.CycleDayErrors),
                typeof(FoodDiary.Application.Abstractions.Dietologist.Common.DietologistErrors),
                typeof(FoodDiary.Application.Abstractions.FavoriteMeals.Common.FavoriteMealErrors),
                typeof(FoodDiary.Application.Abstractions.FavoriteProducts.Common.FavoriteProductErrors),
                typeof(FoodDiary.Application.Abstractions.FavoriteRecipes.Common.FavoriteRecipeErrors),
                typeof(FoodDiary.Application.Abstractions.Images.Common.ImageErrors),
                typeof(FoodDiary.Application.Abstractions.Lessons.Common.LessonErrors),
                typeof(FoodDiary.Application.Abstractions.Admin.Common.AdminMailInboxErrors),
                typeof(FoodDiary.Application.Abstractions.Meals.Common.MealErrors),
                typeof(FoodDiary.Application.Abstractions.MealPlans.Common.MealPlanErrors),
                typeof(FoodDiary.Application.Abstractions.Products.Common.ProductErrors),
                typeof(FoodDiary.Application.Abstractions.Recipes.Common.RecipeErrors),
                typeof(FoodDiary.Application.Abstractions.RecipeComments.Common.RecipeCommentErrors),
                typeof(FoodDiary.Application.Abstractions.ShoppingLists.Common.ShoppingListErrors),
                typeof(FoodDiary.Application.Abstractions.Usda.Common.UsdaErrors),
                typeof(FoodDiary.Application.Abstractions.Users.Common.UserErrors),
                typeof(FoodDiary.Application.Abstractions.Wearables.Common.WearableErrors),
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
