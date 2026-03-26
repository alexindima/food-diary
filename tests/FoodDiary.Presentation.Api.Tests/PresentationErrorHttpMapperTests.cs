using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class PresentationErrorHttpMapperTests {
    [Theory]
    [InlineData(nameof(CreateTelegramInvalidData), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(CreateTelegramBotNotConfigured), StatusCodes.Status500InternalServerError)]
    [InlineData(nameof(CreateTelegramBotInvalidSecret), StatusCodes.Status401Unauthorized)]
    [InlineData(nameof(CreateAdminSsoForbidden), StatusCodes.Status403Forbidden)]
    [InlineData(nameof(CreateAccountNotDeleted), StatusCodes.Status409Conflict)]
    [InlineData(nameof(CreateAiInvalidResponse), StatusCodes.Status502BadGateway)]
    [InlineData(nameof(CreateAiImageNotFound), StatusCodes.Status404NotFound)]
    [InlineData(nameof(CreateAiEmptyItems), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(CreateImageInvalidData), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(CreateImageInUse), StatusCodes.Status409Conflict)]
    [InlineData(nameof(CreateUserInvalidPassword), StatusCodes.Status401Unauthorized)]
    [InlineData(nameof(CreateUserInvalidCredentials), StatusCodes.Status401Unauthorized)]
    [InlineData(nameof(CreateUserEmailAlreadyExists), StatusCodes.Status409Conflict)]
    [InlineData(nameof(CreateValidationConflict), StatusCodes.Status409Conflict)]
    [InlineData(nameof(CreateAuthenticationInvalidToken), StatusCodes.Status401Unauthorized)]
    [InlineData(nameof(CreateValidationRequired), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(CreateProductNotAccessible), StatusCodes.Status404NotFound)]
    [InlineData(nameof(CreateRecipeAlreadyExistsLegacy), StatusCodes.Status409Conflict)]
    [InlineData(nameof(CreateUserNotFound), StatusCodes.Status404NotFound)]
    [InlineData(nameof(CreateUnknownError), StatusCodes.Status500InternalServerError)]
    public void MapStatusCode_ReturnsExpectedStatusCode(string factoryName, int expectedStatusCode) {
        var error = CreateError(factoryName);

        var actual = PresentationErrorHttpMapper.MapStatusCode(error);

        Assert.Equal(expectedStatusCode, actual);
    }

    private static Error CreateError(string factoryName) =>
        factoryName switch {
            nameof(CreateTelegramInvalidData) => CreateTelegramInvalidData(),
            nameof(CreateTelegramBotNotConfigured) => CreateTelegramBotNotConfigured(),
            nameof(CreateTelegramBotInvalidSecret) => CreateTelegramBotInvalidSecret(),
            nameof(CreateAdminSsoForbidden) => CreateAdminSsoForbidden(),
            nameof(CreateAccountNotDeleted) => CreateAccountNotDeleted(),
            nameof(CreateAiInvalidResponse) => CreateAiInvalidResponse(),
            nameof(CreateAiImageNotFound) => CreateAiImageNotFound(),
            nameof(CreateAiEmptyItems) => CreateAiEmptyItems(),
            nameof(CreateImageInvalidData) => CreateImageInvalidData(),
            nameof(CreateImageInUse) => CreateImageInUse(),
            nameof(CreateUserInvalidPassword) => CreateUserInvalidPassword(),
            nameof(CreateUserInvalidCredentials) => CreateUserInvalidCredentials(),
            nameof(CreateUserEmailAlreadyExists) => CreateUserEmailAlreadyExists(),
            nameof(CreateValidationConflict) => CreateValidationConflict(),
            nameof(CreateAuthenticationInvalidToken) => CreateAuthenticationInvalidToken(),
            nameof(CreateValidationRequired) => CreateValidationRequired(),
            nameof(CreateProductNotAccessible) => CreateProductNotAccessible(),
            nameof(CreateRecipeAlreadyExistsLegacy) => CreateRecipeAlreadyExistsLegacy(),
            nameof(CreateUserNotFound) => CreateUserNotFound(),
            nameof(CreateUnknownError) => CreateUnknownError(),
            _ => throw new InvalidOperationException($"Unknown test factory: {factoryName}")
        };

    private static Error CreateTelegramInvalidData() => Errors.Authentication.TelegramInvalidData;
    private static Error CreateTelegramBotNotConfigured() => Errors.Authentication.TelegramBotNotConfigured;
    private static Error CreateTelegramBotInvalidSecret() => Errors.Authentication.TelegramBotInvalidSecret;
    private static Error CreateAdminSsoForbidden() => Errors.Authentication.AdminSsoForbidden;
    private static Error CreateAccountNotDeleted() => Errors.Authentication.AccountNotDeleted;
    private static Error CreateAiInvalidResponse() => Errors.Ai.InvalidResponse("bad response");
    private static Error CreateAiImageNotFound() => Errors.Ai.ImageNotFound(Guid.Empty);
    private static Error CreateAiEmptyItems() => Errors.Ai.EmptyItems();
    private static Error CreateImageInvalidData() => Errors.Image.InvalidData("invalid");
    private static Error CreateImageInUse() => Errors.Image.InUse();
    private static Error CreateUserInvalidPassword() => Errors.User.InvalidPassword;
    private static Error CreateUserInvalidCredentials() => Errors.User.InvalidCredentials;
    private static Error CreateUserEmailAlreadyExists() => Errors.User.EmailAlreadyExists;
    private static Error CreateValidationConflict() => new("Validation.Conflict", "Failure", kind: ErrorKindResolver.Resolve("Validation.Conflict"));
    private static Error CreateAuthenticationInvalidToken() => Errors.Authentication.InvalidToken;
    private static Error CreateValidationRequired() => Errors.Validation.Required("field");
    private static Error CreateProductNotAccessible() => Errors.Product.NotAccessible(Guid.Empty);
    private static Error CreateRecipeAlreadyExistsLegacy() => new("Recipe.AlreadyExists", "Failure", kind: ErrorKindResolver.Resolve("Recipe.AlreadyExists"));
    private static Error CreateUserNotFound() => Errors.User.NotFound();
    private static Error CreateUnknownError() => new("Unknown.Error", "Failure");
}
