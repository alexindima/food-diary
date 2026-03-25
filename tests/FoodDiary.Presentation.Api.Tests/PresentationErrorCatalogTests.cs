using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class PresentationErrorCatalogTests {
    [Fact]
    public void CentralErrorCatalog_HasExpectedHttpCoverage() {
        var internalOnlyCodes = new HashSet<string>(StringComparer.Ordinal) {
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

    private static IReadOnlyList<Error> GetCatalogErrors() => [
        Errors.Product.NotFound(Guid.Empty),
        Errors.Product.NotAccessible(Guid.Empty),
        Errors.Product.AlreadyExists("123"),
        Errors.Product.InvalidData("invalid"),
        Errors.Recipe.NotFound(Guid.Empty),
        Errors.Recipe.NotAccessible(Guid.Empty),
        Errors.Recipe.InvalidData("invalid"),
        Errors.Consumption.NotFound(Guid.Empty),
        Errors.Consumption.InvalidData("invalid"),
        Errors.User.NotFound(Guid.Empty),
        Errors.User.NotFound(),
        Errors.User.InvalidPassword,
        Errors.User.InvalidCredentials,
        Errors.User.EmailAlreadyExists,
        Errors.Authentication.InvalidCredentials,
        Errors.Authentication.InvalidToken,
        Errors.Authentication.AccountDeleted,
        Errors.Authentication.AccountNotDeleted,
        Errors.Authentication.TelegramInvalidData,
        Errors.Authentication.TelegramAuthExpired,
        Errors.Authentication.TelegramNotLinked,
        Errors.Authentication.TelegramAlreadyLinked,
        Errors.Authentication.TelegramNotConfigured,
        Errors.Authentication.TelegramBotNotConfigured,
        Errors.Authentication.TelegramBotInvalidSecret,
        Errors.Authentication.AdminSsoInvalidCode,
        Errors.Authentication.AdminSsoForbidden,
        Errors.Validation.Required("field"),
        Errors.Validation.Invalid("field", "reason"),
        Errors.WeightEntry.NotFound(Guid.Empty),
        Errors.WeightEntry.AlreadyExists(DateTime.UnixEpoch),
        Errors.WaistEntry.NotFound(Guid.Empty),
        Errors.WaistEntry.AlreadyExists(DateTime.UnixEpoch),
        Errors.HydrationEntry.NotFound(Guid.Empty),
        Errors.DailyAdvice.NotFound(),
        Errors.ShoppingList.NotFound(Guid.Empty),
        Errors.ShoppingList.CurrentNotFound(),
        Errors.Cycle.NotFound(Guid.Empty),
        Errors.CycleDay.NotFound(DateTime.UnixEpoch),
        Errors.Ai.ImageNotFound(Guid.Empty),
        Errors.Ai.Forbidden(),
        Errors.Ai.EmptyItems(),
        Errors.Ai.OpenAiFailed("failed"),
        Errors.Ai.InvalidResponse("invalid"),
        Errors.Ai.QuotaExceeded(),
        Errors.Image.InvalidData("invalid"),
        Errors.Image.NotFound(Guid.Empty),
        Errors.Image.Forbidden(),
        Errors.Image.InUse(),
        Errors.Image.StorageError(),
    ];
}
