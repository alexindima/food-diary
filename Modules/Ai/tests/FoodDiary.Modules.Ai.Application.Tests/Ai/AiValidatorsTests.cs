using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Application.Services;
using FoodDiary.Modules.Ai.Application.Commands.AnalyzeFoodImage;
using FoodDiary.Modules.Images.Application.Services;
using FoodDiary.Modules.Ai.Application.Commands.CalculateFoodNutrition;
using FoodDiary.Modules.Ai.Application.Commands.ParseFoodText;

using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Models;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Application.Queries.GetUserAiUsageSummary;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FluentValidation.Results;

namespace FoodDiary.Modules.Ai.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public class AiValidatorsTests {
    [Fact]
    public async Task AnalyzeFoodImageHandler_MapsExplicitForbiddenWithoutCallingProvider() {
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        images.GetDataUrlAsync(Arg.Any<ImageAssetId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(new Error("Image.Forbidden", "Denied", ErrorKind.Forbidden)));
        IOpenAiFoodClient provider = CreateOpenAiFoodService(out OpenAiFoodServiceCalls calls);
        var handler = new AnalyzeFoodImageCommandHandler(images, CreateOrchestration(provider, CreateUserAiProfileReadService(user: null)));

        Result<FoodVisionModel> result = await handler.Handle(new AnalyzeFoodImageCommand(Guid.NewGuid(), Guid.NewGuid(), Description: null, RequestId), CancellationToken.None);

        Assert.Equal("Ai.Forbidden", result.Error.Code);
        Assert.False(calls.WasAnalyzeFoodImageCalled);
    }
    private const string RequestId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task AnalyzeFoodImageValidator_WithEmptyIds_Fails() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(Guid.Empty, Guid.Empty, Description: null, RequestId);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageValidator_WithTooLongDescription_Fails() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 2049), RequestId);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageValidator_WithValidData_Passes() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(Guid.NewGuid(), Guid.NewGuid(), "some context", RequestId);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageValidator_WithNonHexRequestId_Fails() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(Guid.NewGuid(), Guid.NewGuid(), Description: null, new string('G', 64));

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var user = User.Create("ai-handler@example.com", "hash");
        var handler = new AnalyzeFoodImageCommandHandler(CreateImageContentService(CreateImageAssetRepository()), CreateOrchestration(CreateOpenAiFoodService(), CreateUserAiProfileReadService(user)));

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, Guid.Empty, Description: null, RequestId),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new AnalyzeFoodImageCommandHandler(CreateImageContentService(CreateImageAssetRepository()), CreateOrchestration(CreateOpenAiFoodService(), CreateUserAiProfileReadService(User.Create("ai-empty-image-user@example.com", "hash"))));

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(Guid.Empty, Guid.NewGuid(), Description: null, RequestId),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenImageAssetMissing_ReturnsImageNotFound() {
        var user = User.Create("ai-missing-image@example.com", "hash");
        var handler = new AnalyzeFoodImageCommandHandler(CreateImageContentService(CreateImageAssetRepository()), CreateOrchestration(CreateOpenAiFoodService(), CreateUserAiProfileReadService(user)));

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, Guid.NewGuid(), Description: null, RequestId),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.ImageNotFound", result.Error.Code);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenImageBelongsToAnotherUser_ReturnsImageNotFound() {
        var owner = User.Create("ai-image-owner@example.com", "hash");
        var requester = User.Create("ai-image-requester@example.com", "hash");
        var asset = ImageAsset.Create(owner.Id, "images/meal.jpg", "https://cdn.example.com/meal.jpg");
        var handler = new AnalyzeFoodImageCommandHandler(CreateImageContentService(CreateImageAssetRepository(asset)), CreateOrchestration(CreateOpenAiFoodService(), CreateUserAiProfileReadService(requester)));

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(requester.Id.Value, asset.Id.Value, Description: null, RequestId),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.ImageNotFound", result.Error.Code);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenUploadedObjectInvalid_ReturnsImageInvalidData() {
        var user = User.Create("ai-invalid-image@example.com", "hash");
        var asset = ImageAsset.Create(user.Id, "images/invalid.jpg", "https://cdn.example.com/invalid.jpg");
        var handler = new AnalyzeFoodImageCommandHandler(CreateImageContentService(CreateImageAssetRepository(asset)), CreateOrchestration(CreateOpenAiFoodService(), CreateUserAiProfileReadService(user)));

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, asset.Id.Value, Description: null, RequestId),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Contains("not been confirmed", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenUserMissing_ReturnsInvalidToken() {
        var userId = UserId.New();
        var asset = ImageAsset.Create(userId, "images/orphan.jpg", "https://cdn.example.com/orphan.jpg");
        asset.Confirm();
        IOpenAiFoodClient openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new AnalyzeFoodImageCommandHandler(CreateImageContentService(CreateImageAssetRepository(asset)), CreateOrchestration(openAiFoodService, CreateUserAiProfileReadService(user: null)));

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(userId.Value, asset.Id.Value, "notes", RequestId),
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
        asset.Confirm();
        IOpenAiFoodClient openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new AnalyzeFoodImageCommandHandler(CreateImageContentService(CreateImageAssetRepository(asset)), CreateOrchestration(openAiFoodService, CreateUserAiProfileReadService(user)));

        Result<FoodVisionModel> result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, asset.Id.Value, "dinner", RequestId),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(openAiCalls.WasAnalyzeFoodImageCalled);
        Assert.Equal(TestImageDataUrl, openAiCalls.LastImageUrl);
        Assert.Equal("ru", openAiCalls.LastLanguage);
        Assert.Equal("dinner", openAiCalls.LastDescription);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithEmptyItems_Fails() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(Guid.NewGuid(), Array.Empty<FoodVisionItemModel>(), RequestId);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithInvalidItem_Fails() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(
            Guid.NewGuid(),
            [new FoodVisionItemModel("", NameLocal: null, 0, "", -1)],
            RequestId);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithValidItems_Passes() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(
            Guid.NewGuid(),
            [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)],
            RequestId);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithEmptyRequestId_Fails() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(
            Guid.NewGuid(),
            [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)],
            RequestId: string.Empty);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
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
            CreateUserAiProfileReadService(User.Create("ai-empty-user@example.com", "hash")),
            CreateAiUsageRepository(),
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 15, 30, 0, DateTimeKind.Utc)));

        Result<UserAiUsageModel> result = await handler.Handle(new GetUserAiUsageSummaryQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new CalculateFoodNutritionCommandHandler(CreateOrchestration(CreateOpenAiFoodService(), CreateUserAiProfileReadService(User.Create("ai-empty-nutrition@example.com", "hash"))));

        Result<FoodNutritionModel> result = await handler.Handle(
            new CalculateFoodNutritionCommand(
                Guid.Empty,
                [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)],
                RequestId),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithEmptyItems_ReturnsEmptyItems() {
        var handler = new CalculateFoodNutritionCommandHandler(CreateOrchestration(CreateOpenAiFoodService(), CreateUserAiProfileReadService(User.Create("ai-empty-items@example.com", "hash"))));

        Result<FoodNutritionModel> result = await handler.Handle(
            new CalculateFoodNutritionCommand(Guid.NewGuid(), [], RequestId),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.EmptyItems", result.Error.Code);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithInactiveUser_ReturnsInvalidToken() {
        var user = User.Create("inactive-ai-nutrition@example.com", "hash");
        user.Deactivate();
        IOpenAiFoodClient openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new CalculateFoodNutritionCommandHandler(CreateOrchestration(openAiFoodService, CreateUserAiProfileReadService(user)));

        Result<FoodNutritionModel> result = await handler.Handle(
            new CalculateFoodNutritionCommand(
                user.Id.Value,
                [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)],
                RequestId),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(openAiCalls.WasCalculateNutritionCalled);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithActiveUser_CalculatesNutrition() {
        var user = User.Create("active-ai-nutrition@example.com", "hash");
        IOpenAiFoodClient openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new CalculateFoodNutritionCommandHandler(CreateOrchestration(openAiFoodService, CreateUserAiProfileReadService(user)));

        Result<FoodNutritionModel> result = await handler.Handle(
            new CalculateFoodNutritionCommand(
                user.Id.Value,
                [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)],
                RequestId),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(openAiCalls.WasCalculateNutritionCalled);
    }

    [Fact]
    public async Task ParseFoodTextHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new ParseFoodTextCommandHandler(CreateOrchestration(CreateOpenAiFoodService(), CreateUserAiProfileReadService(User.Create("ai-empty-text-user@example.com", "hash"))), CreateCurrentUserAccessService(User.Create("ai-empty-text-user@example.com", "hash")));

        Result<FoodVisionModel> result = await handler.Handle(new ParseFoodTextCommand(Guid.Empty, "apple", RequestId), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ParseFoodTextHandler_WhenUserMissing_ReturnsInvalidToken() {
        IOpenAiFoodClient openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new ParseFoodTextCommandHandler(CreateOrchestration(openAiFoodService, CreateUserAiProfileReadService(user: null)), CreateCurrentUserAccessService(user: null));

        Result<FoodVisionModel> result = await handler.Handle(new ParseFoodTextCommand(Guid.NewGuid(), "apple", RequestId), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(openAiCalls.WasParseFoodTextCalled);
    }

    [Fact]
    public async Task ParseFoodTextHandler_WhenUserAiProfileModelFails_ReturnsFailureWithoutCallingOpenAi() {
        var user = User.Create("ai-context-fails@example.com", "hash");
        IOpenAiFoodClient openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new ParseFoodTextCommandHandler(CreateOrchestration(openAiFoodService, CreateUserAiProfileReadService(user: null)), CreateCurrentUserAccessService(user));

        Result<FoodVisionModel> result = await handler.Handle(new ParseFoodTextCommand(user.Id.Value, "apple", RequestId), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(openAiCalls.WasParseFoodTextCalled);
    }

    [Fact]
    public async Task ParseFoodTextHandler_WithActiveUser_ParsesText() {
        var user = User.Create("active-ai-text@example.com", "hash");
        user.SetLanguage("ru");
        IOpenAiFoodClient openAiFoodService = CreateOpenAiFoodService(out OpenAiFoodServiceCalls openAiCalls);
        var handler = new ParseFoodTextCommandHandler(CreateOrchestration(openAiFoodService, CreateUserAiProfileReadService(user)), CreateCurrentUserAccessService(user));

        Result<FoodVisionModel> result = await handler.Handle(new ParseFoodTextCommand(user.Id.Value, "apple 100g", RequestId), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(openAiCalls.WasParseFoodTextCalled);
        Assert.Equal("apple 100g", openAiCalls.LastText);
        Assert.Equal("ru", openAiCalls.LastLanguage);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryQueryHandler_UsesDateTimeProviderForMonthBounds() {
        var user = User.Create("ai-usage@example.com", "hash");
        IUserAiProfileReadService userProfileReadService = CreateUserAiProfileReadService(user);
        IAiUsageQuery aiUsageRepository = CreateAiUsageRepository(out Func<(DateTime FromUtc, DateTime ToUtc)> getLastPeriod);
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 3, 26, 15, 30, 0, DateTimeKind.Utc));
        var handler = new GetUserAiUsageSummaryQueryHandler(userProfileReadService, aiUsageRepository, dateTimeProvider);

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
            CreateUserAiProfileReadService(user),
            CreateAiUsageRepository(),
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 15, 30, 0, DateTimeKind.Utc)));

        Result<UserAiUsageModel> result = await handler.Handle(new GetUserAiUsageSummaryQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Theory]
    [InlineData("vision", true)]
    [InlineData("text-parse", true)]
    [InlineData("nutrition", true)]
    [InlineData("vision", false)]
    [InlineData("text-parse", false)]
    [InlineData("nutrition", false)]
    public async Task HandlerWorkflow_ReadsProfileOnceAndPreservesConsentAndLanguage(string operation, bool consent) {
        var user = User.Create("single-profile@example.com", "hash");
        IUserAiProfileReadService profiles = Substitute.For<IUserAiProfileReadService>();
        profiles.GetAiProfileAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserAiProfileModel(user.Id, "ru", 1000, 2000, consent)));
        IOpenAiFoodClient client = CreateOpenAiFoodService(out OpenAiFoodServiceCalls calls);
        IAiQuotaRepository quota = Substitute.For<IAiQuotaRepository>();
        IAiPromptProvider prompts = Substitute.For<IAiPromptProvider>();
        OpenAiFoodService service = CreateOrchestration(client, profiles, quota, prompts);
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        images.GetDataUrlAsync(Arg.Any<ImageAssetId>(), user.Id, Arg.Any<CancellationToken>()).Returns(Result.Success(TestImageDataUrl));
        using var cancellation = new CancellationTokenSource();
        Result result = operation switch {
            "vision" => await new AnalyzeFoodImageCommandHandler(images, service).Handle(
                new AnalyzeFoodImageCommand(user.Id.Value, Guid.NewGuid(), "meal", RequestId), cancellation.Token),
            "text-parse" => await new ParseFoodTextCommandHandler(service, CreateCurrentUserAccessService(user)).Handle(
                new ParseFoodTextCommand(user.Id.Value, "apple", RequestId), cancellation.Token),
            _ => await new CalculateFoodNutritionCommandHandler(service).Handle(
                new CalculateFoodNutritionCommand(user.Id.Value, [new FoodVisionItemModel("apple", "apple", 100, "g", 1m)], RequestId), cancellation.Token),
        };
        await profiles.Received(1).GetAiProfileAsync(user.Id, Arg.Is<CancellationToken>(token => token.CanBeCanceled));
        await prompts.Received(consent ? 1 : 0).GetPromptAsync(operation, "ru", Arg.Is<CancellationToken>(token => token.CanBeCanceled));
        await quota.Received(consent ? 1 : 0).ReserveAsync(
            Arg.Is<AiQuotaReservationRequest>(request => request.UserId == user.Id && request.InputTokenLimit == 1000 && request.OutputTokenLimit == 2000),
            Arg.Any<CancellationToken>());
        Assert.Equal(consent, result.IsSuccess);
        if (!consent) {
            Assert.Equal("Ai.ConsentRequired", result.Error.Code);
            Assert.Multiple(
                () => Assert.False(calls.WasAnalyzeFoodImageCalled),
                () => Assert.False(calls.WasParseFoodTextCalled),
                () => Assert.False(calls.WasCalculateNutritionCalled));
        } else if (!string.Equals(operation, "nutrition", StringComparison.Ordinal)) {
            Assert.Equal("ru", calls.LastLanguage);
        }
    }

    private static IUserAiProfileReadService CreateUserAiProfileReadService(User? user) {
        IUserAiProfileReadService service = Substitute.For<IUserAiProfileReadService>();
        service
            .GetAiProfileAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                if (user is null || user.Id != id) {
                    return Task.FromResult(Result.Failure<UserAiProfileModel>(AuthenticationErrors.InvalidToken));
                }

                if (!user.IsActive || user.DeletedAt is not null) {
                    return Task.FromResult(Result.Failure<UserAiProfileModel>(AuthenticationErrors.InvalidToken));
                }

                return Task.FromResult(Result.Success(new UserAiProfileModel(
                    user.Id,
                    user.Language,
                    user.AiInputTokenLimit,
                    user.AiOutputTokenLimit,
                    HasAcceptedAiConsent: true)));
            });
        return service;
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User? user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                Error? error = user is null || user.Id != id || !user.IsActive || user.DeletedAt is not null
                    ? AuthenticationErrors.InvalidToken
                    : null;
                return Task.FromResult(error);
            });
        return service;
    }

    private const string TestImageDataUrl = "data:image/png;base64,AQID";

    private static IImageAssetContentService CreateImageContentService(IImageAssetReadRepository repository) {
        var access = new ImageAssetAccessService(repository);
        IImageAssetContentService content = Substitute.For<IImageAssetContentService>();
        content.GetDataUrlAsync(Arg.Any<ImageAssetId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(async call => {
                Result<ImageAssetReadModel?> result = await access.ResolveOptionalAsync(call.ArgAt<ImageAssetId>(0), call.ArgAt<UserId>(1), call.ArgAt<CancellationToken>(2));
                return result.IsFailure ? Result.Failure<string>(result.Error) : Result.Success(TestImageDataUrl);
            });
        return content;
    }

    private static IImageAssetRepository CreateImageAssetRepository(ImageAsset? asset = null) {
        IImageAssetRepository repository = Substitute.For<IImageAssetRepository>();
        ((IImageAssetReadRepository)repository)
            .GetOwnedByIdAsync(Arg.Any<ImageAssetId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                ImageAssetId id = call.Arg<ImageAssetId>();
                UserId userId = call.Arg<UserId>();
                return Task.FromResult(asset is not null && asset.Id == id && asset.UserId == userId ? asset : null);
            });
        return repository;
    }

    private static IOpenAiFoodClient CreateOpenAiFoodService() => CreateOpenAiFoodService(out _);

    private static OpenAiFoodService CreateOrchestration(IOpenAiFoodClient client, IUserAiProfileReadService users, IAiQuotaRepository? quota = null, IAiPromptProvider? prompts = null) {
        quota ??= Substitute.For<IAiQuotaRepository>();
        quota.ReserveAsync(Arg.Any<AiQuotaReservationRequest>(), Arg.Any<CancellationToken>()).Returns(AiQuotaReservationStatus.Acquired);
        prompts ??= Substitute.For<IAiPromptProvider>();
        prompts.GetPromptAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns("prompt");
        return new OpenAiFoodService(client, quota, users, TimeProvider.System, prompts);
    }

    private static IOpenAiFoodClient CreateOpenAiFoodService(out OpenAiFoodServiceCalls calls) {
        calls = new OpenAiFoodServiceCalls();
        OpenAiFoodServiceCalls capturedCalls = calls;
        IOpenAiFoodClient client = Substitute.For<IOpenAiFoodClient>();
        var budget = Result.Success(new AiProviderTokenBudget(10, 10));
        client.GetAnalyzeFoodImageTokenBudgetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(budget);
        client.GetParseFoodTextTokenBudgetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(budget);
        client.GetCalculateNutritionTokenBudgetAsync(Arg.Any<IReadOnlyList<FoodVisionItemModel>>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(budget);
        var vision = new FoodVisionModel([new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)], Notes: null);
        client.AnalyzeFoodImageAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                capturedCalls.WasAnalyzeFoodImageCalled = true;
                capturedCalls.LastImageUrl = call.ArgAt<string>(0);
                capturedCalls.LastLanguage = call.ArgAt<string?>(1);
                capturedCalls.LastDescription = call.ArgAt<string?>(2);
                return Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(vision, "vision", "test", new AiUsageTokens(1, 1, 2)));
            });
        client.ParseFoodTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                capturedCalls.WasParseFoodTextCalled = true;
                capturedCalls.LastText = call.ArgAt<string>(0);
                capturedCalls.LastLanguage = call.ArgAt<string?>(1);
                return Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(vision, "text-parse", "test", new AiUsageTokens(1, 1, 2)));
            });
        client.CalculateNutritionAsync(Arg.Any<IReadOnlyList<FoodVisionItemModel>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                capturedCalls.WasCalculateNutritionCalled = true;
                var nutrition = new FoodNutritionModel(52, 0, 0, 14, 2, 0, [new FoodNutritionItemModel("apple", 120, "g", 52, 0, 0, 14, 2, 0)]);
                return Result.Success(new OpenAiFoodClientResponse<FoodNutritionModel>(nutrition, "nutrition", "test", new AiUsageTokens(1, 1, 2)));
            });
        return client;
    }

    private static IAiUsageQuery CreateAiUsageRepository() =>
        CreateAiUsageRepository(out _);

    private static IAiUsageQuery CreateAiUsageRepository(
        out Func<(DateTime FromUtc, DateTime ToUtc)> getLastPeriod) {
        DateTime lastFromUtc = default;
        DateTime lastToUtc = default;
        IAiUsageQuery repository = Substitute.For<IAiUsageQuery>();
        ((IAiUsageQuery)repository)
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
