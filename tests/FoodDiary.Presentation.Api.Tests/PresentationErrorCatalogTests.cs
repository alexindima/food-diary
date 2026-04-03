using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class PresentationErrorCatalogTests {
    [Fact]
    public void CentralErrorCatalog_HasExpectedHttpCoverage() {
        var internalOnlyCodes = new HashSet<string>(StringComparer.Ordinal) {
            "Authentication.GoogleNotConfigured",
            "Authentication.TelegramNotConfigured",
            "Authentication.TelegramBotNotConfigured",
            "Consumption.InvalidData",
            "Product.InvalidData",
            "Recipe.InvalidData",
        };

        foreach (var error in GetCatalogErrors()) {
            var statusCode = PresentationErrorHttpMapper.MapStatusCode(error);

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
    public void CentralErrorCatalog_DefinesErrorKind_ForAllPublishedErrors() {
        var missingKinds = GetCatalogErrors()
            .Where(static error => error.Kind is null)
            .Select(static error => error.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(missingKinds);
    }

    [Fact]
    public void CentralErrorCatalog_CanBeEnumeratedWithoutDuplicatesOrMissingCodes() {
        var duplicates = GetCatalogErrors()
            .GroupBy(static error => error.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Where(group => group.Key is not "User.NotFound" and not "ShoppingList.NotFound")
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicates);
    }

    private static IReadOnlyList<Error> GetCatalogErrors() {
        return typeof(Errors)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(GetErrorsFromType)
            .OrderBy(static error => error.Code, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<Error> GetErrorsFromType(Type type) {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static)) {
            if (property.PropertyType != typeof(Error) || property.GetIndexParameters().Length > 0) {
                continue;
            }

            if (property.GetValue(null) is Error error) {
                yield return error;
            }
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if (method.ReturnType != typeof(Error)) {
                continue;
            }

            if (method.IsSpecialName) {
                continue;
            }

            var arguments = method.GetParameters()
                .Select(CreateSampleArgument)
                .ToArray();

            if (method.Invoke(null, arguments) is Error error) {
                yield return error;
            }
        }
    }

    private static object? CreateSampleArgument(ParameterInfo parameter) {
        var parameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

        if (parameterType == typeof(Guid)) {
            return Guid.Empty;
        }

        if (parameterType == typeof(DateTime)) {
            return DateTime.UnixEpoch;
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
