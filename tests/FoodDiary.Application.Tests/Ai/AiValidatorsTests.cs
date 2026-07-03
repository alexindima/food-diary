using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Application.Ai.Commands.ParseFoodText;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation.Results;

namespace FoodDiary.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public class AiValidatorsTests {
    [Fact]
    public async Task AnalyzeFoodImageValidator_WithEmptyIds_Fails() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(Guid.Empty, Guid.Empty, Description: null);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageValidator_WithTooLongDescription_Fails() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 2049));

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageValidator_WithValidData_Passes() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(Guid.NewGuid(), Guid.NewGuid(), "some context");

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var user = User.Create("ai-handler@example.com", "hash");
        var handler = new AnalyzeFoodImageCommandHandler(
            CreateImageAssetRepository(),
            CreateAiUserContextService(user),
            CreateOpenAiFoodService(),
            CreateImageStorageService());

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, Guid.Empty, Description: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new AnalyzeFoodImageCommandHandler(
            CreateImageAssetRepository(),
            CreateAiUserContextService(User.Create("ai-empty-image-user@example.com", "hash")),
            CreateOpenAiFoodService(),
            CreateImageStorageService());

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(Guid.Empty, Guid.NewGuid(), Description: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenImageAssetMissing_ReturnsImageNotFound() {
        var user = User.Create("ai-missing-image@example.com", "hash");
        var handler = new AnalyzeFoodImageCommandHandler(
            CreateImageAssetRepository(),
            CreateAiUserContextService(user),
            CreateOpenAiFoodService(),
            CreateImageStorageService());

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, Guid.NewGuid(), Description: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.ImageNotFound", result.Error.Code);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenImageBelongsToAnotherUser_ReturnsForbidden() {
        var owner = User.Create("ai-image-owner@example.com", "hash");
        var requester = User.Create("ai-image-requester@example.com", "hash");
        var asset = ImageAsset.Create(owner.Id, "images/meal.jpg", "https://cdn.example.com/meal.jpg");
        var handler = new AnalyzeFoodImageCommandHandler(
            CreateImageAssetRepository(asset),
            CreateAiUserContextService(requester),
            CreateOpenAiFoodService(),
            CreateImageStorageService());

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(requester.Id.Value, asset.Id.Value, Description: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenUploadedObjectInvalid_ReturnsImageInvalidData() {
        var user = User.Create("ai-invalid-image@example.com", "hash");
        var asset = ImageAsset.Create(user.Id, "images/invalid.jpg", "https://cdn.example.com/invalid.jpg");
        var handler = new AnalyzeFoodImageCommandHandler(
            CreateImageAssetRepository(asset),
            CreateAiUserContextService(user),
            CreateOpenAiFoodService(),
            CreateImageStorageService(isValid: false, message: "upload incomplete"));

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, asset.Id.Value, Description: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Contains("upload incomplete", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenUserMissing_ReturnsInvalidToken() {
        var userId = UserId.New();
        var asset = ImageAsset.Create(userId, "images/orphan.jpg", "https://cdn.example.com/orphan.jpg");
        IOpenAiFoodService openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new AnalyzeFoodImageCommandHandler(
            CreateImageAssetRepository(asset),
            CreateAiUserContextService(user: null),
            openAiFoodService,
            CreateImageStorageService());

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(userId.Value, asset.Id.Value, "notes"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(openAiCalls.WasAnalyzeFoodImageCalled);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WithValidImage_CallsOpenAiFoodService() {
        var user = User.Create("ai-valid-image@example.com", "hash");
        user.SetLanguage("ru");
        var asset = ImageAsset.Create(user.Id, "images/valid.jpg", "https://cdn.example.com/valid.jpg");
        IOpenAiFoodService openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new AnalyzeFoodImageCommandHandler(
            CreateImageAssetRepository(asset),
            CreateAiUserContextService(user),
            openAiFoodService,
            CreateImageStorageService());

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, asset.Id.Value, "dinner"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(openAiCalls.WasAnalyzeFoodImageCalled);
        Assert.Equal(asset.Url, openAiCalls.LastImageUrl);
        Assert.Equal("ru", openAiCalls.LastLanguage);
        Assert.Equal("dinner", openAiCalls.LastDescription);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithEmptyItems_Fails() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(Guid.NewGuid(), Array.Empty<FoodVisionItemModel>());

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithInvalidItem_Fails() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(
            Guid.NewGuid(),
            [new FoodVisionItemModel("", NameLocal: null, 0, "", -1)]);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithValidItems_Passes() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(
            Guid.NewGuid(),
            [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)]);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryValidator_WithEmptyUserId_Fails() {
        var validator = new GetUserAiUsageSummaryQueryValidator();
        var query = new GetUserAiUsageSummaryQuery(Guid.Empty);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryValidator_WithValidUserId_Passes() {
        var validator = new GetUserAiUsageSummaryQueryValidator();
        var query = new GetUserAiUsageSummaryQuery(Guid.NewGuid());

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryQueryHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new GetUserAiUsageSummaryQueryHandler(
            CreateAiUserContextService(User.Create("ai-empty-user@example.com", "hash")),
            CreateAiUsageRepository(),
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 15, 30, 0, DateTimeKind.Utc)));

        Result<UserAiUsageModel> result = await handler.Handle(new GetUserAiUsageSummaryQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AiUserContextService_WhenUserMissing_ReturnsAccessFailure() {
        IUserContextService userContextService = Substitute.For<IUserContextService>();
        userContextService
            .GetAccessibleUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<User>(Errors.Authentication.InvalidToken));
        var service = new AiUserContextService(userContextService);

        Result<AiUserContext> result = await service.GetAsync(UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new CalculateFoodNutritionCommandHandler(
            CreateOpenAiFoodService(),
            CreateAiUserContextService(User.Create("ai-empty-nutrition@example.com", "hash")));

        Result<FoodNutritionModel> result = await handler.Handle(
            new CalculateFoodNutritionCommand(
                Guid.Empty,
                [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithEmptyItems_ReturnsEmptyItems() {
        var handler = new CalculateFoodNutritionCommandHandler(
            CreateOpenAiFoodService(),
            CreateAiUserContextService(User.Create("ai-empty-items@example.com", "hash")));

        Result<FoodNutritionModel> result = await handler.Handle(
            new CalculateFoodNutritionCommand(Guid.NewGuid(), []),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.EmptyItems", result.Error.Code);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithInactiveUser_ReturnsInvalidToken() {
        var user = User.Create("inactive-ai-nutrition@example.com", "hash");
        user.Deactivate();
        IOpenAiFoodService openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new CalculateFoodNutritionCommandHandler(openAiFoodService, CreateAiUserContextService(user));

        Result<FoodNutritionModel> result = await handler.Handle(
            new CalculateFoodNutritionCommand(
                user.Id.Value,
                [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(openAiCalls.WasCalculateNutritionCalled);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithActiveUser_CalculatesNutrition() {
        var user = User.Create("active-ai-nutrition@example.com", "hash");
        IOpenAiFoodService openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new CalculateFoodNutritionCommandHandler(openAiFoodService, CreateAiUserContextService(user));

        Result<FoodNutritionModel> result = await handler.Handle(
            new CalculateFoodNutritionCommand(
                user.Id.Value,
                [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(openAiCalls.WasCalculateNutritionCalled);
    }

    [Fact]
    public async Task ParseFoodTextHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new ParseFoodTextCommandHandler(
            CreateOpenAiFoodService(),
            CreateAiUserContextService(User.Create("ai-empty-text-user@example.com", "hash")));

        Result<FoodVisionModel> result = await handler.Handle(new ParseFoodTextCommand(Guid.Empty, "apple"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ParseFoodTextHandler_WhenUserMissing_ReturnsInvalidToken() {
        IOpenAiFoodService openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new ParseFoodTextCommandHandler(openAiFoodService, CreateAiUserContextService(user: null));

        Result<FoodVisionModel> result = await handler.Handle(new ParseFoodTextCommand(Guid.NewGuid(), "apple"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(openAiCalls.WasParseFoodTextCalled);
    }

    [Fact]
    public async Task ParseFoodTextHandler_WithActiveUser_ParsesText() {
        var user = User.Create("active-ai-text@example.com", "hash");
        user.SetLanguage("ru");
        IOpenAiFoodService openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new ParseFoodTextCommandHandler(openAiFoodService, CreateAiUserContextService(user));

        Result<FoodVisionModel> result = await handler.Handle(new ParseFoodTextCommand(user.Id.Value, "apple 100g"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(openAiCalls.WasParseFoodTextCalled);
        Assert.Equal("apple 100g", openAiCalls.LastText);
        Assert.Equal("ru", openAiCalls.LastLanguage);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryQueryHandler_UsesDateTimeProviderForMonthBounds() {
        var user = User.Create("ai-usage@example.com", "hash");
        IAiUserContextService aiUserContextService = CreateAiUserContextService(user);
        IAiUsageRepository aiUsageRepository = CreateAiUsageRepository(out Func<(DateTime FromUtc, DateTime ToUtc)> getLastPeriod);
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 3, 26, 15, 30, 0, DateTimeKind.Utc));
        var handler = new GetUserAiUsageSummaryQueryHandler(aiUserContextService, aiUsageRepository, dateTimeProvider);

        Result<UserAiUsageModel> result = await handler.Handle(new GetUserAiUsageSummaryQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), getLastPeriod().FromUtc);
        Assert.Equal(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), getLastPeriod().ToUtc);
        Assert.Equal(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), result.Value.ResetAtUtc);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryQueryHandler_WithInactiveUser_ReturnsInvalidToken() {
        var user = User.Create("inactive-ai@example.com", "hash");
        user.Deactivate();
        var handler = new GetUserAiUsageSummaryQueryHandler(
            CreateAiUserContextService(user),
            CreateAiUsageRepository(),
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 15, 30, 0, DateTimeKind.Utc)));

        Result<UserAiUsageModel> result = await handler.Handle(new GetUserAiUsageSummaryQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    private static IAiUserContextService CreateAiUserContextService(User? user) {
        IAiUserContextService service = Substitute.For<IAiUserContextService>();
        service
            .GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                if (user is null || user.Id != id) {
                    return Task.FromResult(Result.Failure<AiUserContext>(Errors.Authentication.InvalidToken));
                }

                if (!user.IsActive || user.DeletedAt is not null) {
                    return Task.FromResult(Result.Failure<AiUserContext>(Errors.Authentication.InvalidToken));
                }

                return Task.FromResult(Result.Success(new AiUserContext(
                    user.Id,
                    user.Language,
                    user.AiInputTokenLimit,
                    user.AiOutputTokenLimit)));
            });
        return service;
    }

    private static IImageAssetRepository CreateImageAssetRepository(ImageAsset? asset = null) {
        IImageAssetRepository repository = Substitute.For<IImageAssetRepository>();
        repository
            .GetByIdAsync(Arg.Any<ImageAssetId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                ImageAssetId id = call.Arg<ImageAssetId>();
                return Task.FromResult(asset is not null && asset.Id == id ? asset : null);
            });
        return repository;
    }

    private static IOpenAiFoodService CreateOpenAiFoodService() =>
        CreateOpenAiFoodService(out _);

    private static IOpenAiFoodService CreateOpenAiFoodService(out OpenAiFoodServiceCalls calls) {
        calls = new OpenAiFoodServiceCalls();
        OpenAiFoodServiceCalls capturedCalls = calls;

        IOpenAiFoodService service = Substitute.For<IOpenAiFoodService>();
        service
            .AnalyzeFoodImageAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<UserId>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(call => {
                capturedCalls.WasAnalyzeFoodImageCalled = true;
                capturedCalls.LastImageUrl = call.ArgAt<string>(0);
                capturedCalls.LastLanguage = call.ArgAt<string?>(1);
                capturedCalls.LastDescription = call.ArgAt<string?>(3);
                return Task.FromResult(Result.Success(new FoodVisionModel(
                    [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)],
                    Notes: null)));
            });
        service
            .ParseFoodTextAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<UserId>(),
                Arg.Any<CancellationToken>())
            .Returns(call => {
                capturedCalls.WasParseFoodTextCalled = true;
                capturedCalls.LastText = call.ArgAt<string>(0);
                capturedCalls.LastLanguage = call.ArgAt<string?>(1);
                return Task.FromResult(Result.Success(new FoodVisionModel(
                    [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)],
                    Notes: null)));
            });
        service
            .CalculateNutritionAsync(
                Arg.Any<IReadOnlyList<FoodVisionItemModel>>(),
                Arg.Any<UserId>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => {
                capturedCalls.WasCalculateNutritionCalled = true;
                return Task.FromResult(Result.Success(new FoodNutritionModel(
                    52,
                    0,
                    0,
                    14,
                    2,
                    0,
                    [new FoodNutritionItemModel("apple", 120, "g", 52, 0, 0, 14, 2, 0)])));
            });

        return service;
    }

    private static IImageStorageService CreateImageStorageService(bool isValid = true, string? message = null) {
        IImageStorageService service = Substitute.For<IImageStorageService>();
        service
            .ValidateUploadedObjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ImageObjectValidationResult(isValid, Message: message)));
        return service;
    }

    private static IAiUsageRepository CreateAiUsageRepository() =>
        CreateAiUsageRepository(out _);

    private static IAiUsageRepository CreateAiUsageRepository(
        out Func<(DateTime FromUtc, DateTime ToUtc)> getLastPeriod) {
        DateTime lastFromUtc = default;
        DateTime lastToUtc = default;
        IAiUsageRepository repository = Substitute.For<IAiUsageRepository>();
        repository
            .GetUserTotalsAsync(
                Arg.Any<UserId>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call => {
                lastFromUtc = call.ArgAt<DateTime>(1);
                lastToUtc = call.ArgAt<DateTime>(2);
                return Task.FromResult(new AiUsageTotals(12, 34));
            });

        getLastPeriod = () => (lastFromUtc, lastToUtc);
        return repository;
    }

    [ExcludeFromCodeCoverage]
    private sealed class OpenAiFoodServiceCalls {
        public bool WasAnalyzeFoodImageCalled { get; set; }
        public bool WasParseFoodTextCalled { get; set; }
        public bool WasCalculateNutritionCalled { get; set; }
        public string? LastImageUrl { get; set; }
        public string? LastText { get; set; }
        public string? LastLanguage { get; set; }
        public string? LastDescription { get; set; }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
