using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class PresentationErrorHttpMapperTests {
    [Theory]
    [InlineData("Authentication.TelegramInvalidData", StatusCodes.Status400BadRequest)]
    [InlineData("Authentication.TelegramBotNotConfigured", StatusCodes.Status500InternalServerError)]
    [InlineData("Authentication.TelegramBotInvalidSecret", StatusCodes.Status401Unauthorized)]
    [InlineData("Authentication.AdminSsoForbidden", StatusCodes.Status403Forbidden)]
    [InlineData("Authentication.AccountNotDeleted", StatusCodes.Status409Conflict)]
    [InlineData("Ai.InvalidResponse", StatusCodes.Status502BadGateway)]
    [InlineData("Ai.ImageNotFound", StatusCodes.Status404NotFound)]
    [InlineData("Ai.EmptyItems", StatusCodes.Status400BadRequest)]
    [InlineData("Image.InvalidData", StatusCodes.Status400BadRequest)]
    [InlineData("Image.InUse", StatusCodes.Status409Conflict)]
    [InlineData("User.InvalidPassword", StatusCodes.Status401Unauthorized)]
    [InlineData("User.InvalidCredentials", StatusCodes.Status401Unauthorized)]
    [InlineData("User.EmailAlreadyExists", StatusCodes.Status409Conflict)]
    [InlineData("Validation.Conflict", StatusCodes.Status409Conflict)]
    [InlineData("Authentication.InvalidToken", StatusCodes.Status401Unauthorized)]
    [InlineData("Validation.Required", StatusCodes.Status400BadRequest)]
    [InlineData("Product.NotAccessible", StatusCodes.Status404NotFound)]
    [InlineData("Recipe.AlreadyExists", StatusCodes.Status409Conflict)]
    [InlineData("User.NotFound", StatusCodes.Status404NotFound)]
    [InlineData("Unknown.Error", StatusCodes.Status500InternalServerError)]
    public void MapStatusCode_ReturnsExpectedStatusCode(string errorCode, int expectedStatusCode) {
        var error = new Error(errorCode, "Failure");

        var actual = PresentationErrorHttpMapper.MapStatusCode(error);

        Assert.Equal(expectedStatusCode, actual);
    }
}
