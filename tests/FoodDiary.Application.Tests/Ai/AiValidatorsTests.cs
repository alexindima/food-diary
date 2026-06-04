using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Application.Ai.Commands.ParseFoodText;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public class AiValidatorsTests {
    [Fact]
    public async Task AnalyzeFoodImageValidator_WithEmptyIds_Fails() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(Guid.Empty, Guid.Empty, null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageValidator_WithTooLongDescription_Fails() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 2049));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageValidator_WithValidData_Passes() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(Guid.NewGuid(), Guid.NewGuid(), "some context");

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var user = User.Create("ai-handler@example.com", "hash");
        var handler = new AnalyzeFoodImageCommandHandler(
            new StubImageAssetRepository(),
            new StubUserRepository(user),
            new StubOpenAiFoodService(),
            new StubImageStorageService());

        var result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, Guid.Empty, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new AnalyzeFoodImageCommandHandler(
            new StubImageAssetRepository(),
            new StubUserRepository(User.Create("ai-empty-image-user@example.com", "hash")),
            new StubOpenAiFoodService(),
            new StubImageStorageService());

        var result = await handler.Handle(
            new AnalyzeFoodImageCommand(Guid.Empty, Guid.NewGuid(), null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenImageAssetMissing_ReturnsImageNotFound() {
        var user = User.Create("ai-missing-image@example.com", "hash");
        var handler = new AnalyzeFoodImageCommandHandler(
            new StubImageAssetRepository(),
            new StubUserRepository(user),
            new StubOpenAiFoodService(),
            new StubImageStorageService());

        var result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, Guid.NewGuid(), null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.ImageNotFound", result.Error.Code);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenImageBelongsToAnotherUser_ReturnsForbidden() {
        var owner = User.Create("ai-image-owner@example.com", "hash");
        var requester = User.Create("ai-image-requester@example.com", "hash");
        var asset = ImageAsset.Create(owner.Id, "images/meal.jpg", "https://cdn.example.com/meal.jpg");
        var handler = new AnalyzeFoodImageCommandHandler(
            new StubImageAssetRepository(asset),
            new StubUserRepository(requester),
            new StubOpenAiFoodService(),
            new StubImageStorageService());

        var result = await handler.Handle(
            new AnalyzeFoodImageCommand(requester.Id.Value, asset.Id.Value, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenUploadedObjectInvalid_ReturnsImageInvalidData() {
        var user = User.Create("ai-invalid-image@example.com", "hash");
        var asset = ImageAsset.Create(user.Id, "images/invalid.jpg", "https://cdn.example.com/invalid.jpg");
        var handler = new AnalyzeFoodImageCommandHandler(
            new StubImageAssetRepository(asset),
            new StubUserRepository(user),
            new StubOpenAiFoodService(),
            new StubImageStorageService(isValid: false, message: "upload incomplete"));

        var result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, asset.Id.Value, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Contains("upload incomplete", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WhenUserMissing_ReturnsInvalidToken() {
        var userId = UserId.New();
        var asset = ImageAsset.Create(userId, "images/orphan.jpg", "https://cdn.example.com/orphan.jpg");
        var openAiFoodService = new StubOpenAiFoodService();
        var handler = new AnalyzeFoodImageCommandHandler(
            new StubImageAssetRepository(asset),
            new StubUserRepository(null),
            openAiFoodService,
            new StubImageStorageService());

        var result = await handler.Handle(
            new AnalyzeFoodImageCommand(userId.Value, asset.Id.Value, "notes"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(openAiFoodService.WasAnalyzeFoodImageCalled);
    }

    [Fact]
    public async Task AnalyzeFoodImageHandler_WithValidImage_CallsOpenAiFoodService() {
        var user = User.Create("ai-valid-image@example.com", "hash");
        user.SetLanguage("ru");
        var asset = ImageAsset.Create(user.Id, "images/valid.jpg", "https://cdn.example.com/valid.jpg");
        var openAiFoodService = new StubOpenAiFoodService();
        var handler = new AnalyzeFoodImageCommandHandler(
            new StubImageAssetRepository(asset),
            new StubUserRepository(user),
            openAiFoodService,
            new StubImageStorageService());

        var result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, asset.Id.Value, "dinner"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(openAiFoodService.WasAnalyzeFoodImageCalled);
        Assert.Equal(asset.Url, openAiFoodService.LastImageUrl);
        Assert.Equal("ru", openAiFoodService.LastLanguage);
        Assert.Equal("dinner", openAiFoodService.LastDescription);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithEmptyItems_Fails() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(Guid.NewGuid(), Array.Empty<FoodVisionItemModel>());

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithInvalidItem_Fails() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(
            Guid.NewGuid(),
            [new FoodVisionItemModel("", null, 0, "", -1)]);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithValidItems_Passes() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(
            Guid.NewGuid(),
            [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)]);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryValidator_WithEmptyUserId_Fails() {
        var validator = new GetUserAiUsageSummaryQueryValidator();
        var query = new GetUserAiUsageSummaryQuery(Guid.Empty);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryValidator_WithValidUserId_Passes() {
        var validator = new GetUserAiUsageSummaryQueryValidator();
        var query = new GetUserAiUsageSummaryQuery(Guid.NewGuid());

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryQueryHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new GetUserAiUsageSummaryQueryHandler(
            new StubUserRepository(User.Create("ai-empty-user@example.com", "hash")),
            new RecordingAiUsageRepository(),
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 15, 30, 0, DateTimeKind.Utc)));

        var result = await handler.Handle(new GetUserAiUsageSummaryQuery(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new CalculateFoodNutritionCommandHandler(
            new StubOpenAiFoodService(),
            new StubUserRepository(User.Create("ai-empty-nutrition@example.com", "hash")));

        var result = await handler.Handle(
            new CalculateFoodNutritionCommand(
                Guid.Empty,
                [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithEmptyItems_ReturnsEmptyItems() {
        var handler = new CalculateFoodNutritionCommandHandler(
            new StubOpenAiFoodService(),
            new StubUserRepository(User.Create("ai-empty-items@example.com", "hash")));

        var result = await handler.Handle(
            new CalculateFoodNutritionCommand(Guid.NewGuid(), []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.EmptyItems", result.Error.Code);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithInactiveUser_ReturnsInvalidToken() {
        var user = User.Create("inactive-ai-nutrition@example.com", "hash");
        user.Deactivate();
        var openAiFoodService = new StubOpenAiFoodService();
        var handler = new CalculateFoodNutritionCommandHandler(openAiFoodService, new StubUserRepository(user));

        var result = await handler.Handle(
            new CalculateFoodNutritionCommand(
                user.Id.Value,
                [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(openAiFoodService.WasCalculateNutritionCalled);
    }

    [Fact]
    public async Task CalculateFoodNutritionHandler_WithActiveUser_CalculatesNutrition() {
        var user = User.Create("active-ai-nutrition@example.com", "hash");
        var openAiFoodService = new StubOpenAiFoodService();
        var handler = new CalculateFoodNutritionCommandHandler(openAiFoodService, new StubUserRepository(user));

        var result = await handler.Handle(
            new CalculateFoodNutritionCommand(
                user.Id.Value,
                [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(openAiFoodService.WasCalculateNutritionCalled);
    }

    [Fact]
    public async Task ParseFoodTextHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new ParseFoodTextCommandHandler(
            new StubOpenAiFoodService(),
            new StubUserRepository(User.Create("ai-empty-text-user@example.com", "hash")));

        var result = await handler.Handle(new ParseFoodTextCommand(Guid.Empty, "apple"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ParseFoodTextHandler_WhenUserMissing_ReturnsInvalidToken() {
        var openAiFoodService = new StubOpenAiFoodService();
        var handler = new ParseFoodTextCommandHandler(openAiFoodService, new StubUserRepository(null));

        var result = await handler.Handle(new ParseFoodTextCommand(Guid.NewGuid(), "apple"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(openAiFoodService.WasParseFoodTextCalled);
    }

    [Fact]
    public async Task ParseFoodTextHandler_WithActiveUser_ParsesText() {
        var user = User.Create("active-ai-text@example.com", "hash");
        user.SetLanguage("ru");
        var openAiFoodService = new StubOpenAiFoodService();
        var handler = new ParseFoodTextCommandHandler(openAiFoodService, new StubUserRepository(user));

        var result = await handler.Handle(new ParseFoodTextCommand(user.Id.Value, "apple 100g"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(openAiFoodService.WasParseFoodTextCalled);
        Assert.Equal("apple 100g", openAiFoodService.LastText);
        Assert.Equal("ru", openAiFoodService.LastLanguage);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryQueryHandler_UsesDateTimeProviderForMonthBounds() {
        var user = User.Create("ai-usage@example.com", "hash");
        var userRepository = new StubUserRepository(user);
        var aiUsageRepository = new RecordingAiUsageRepository();
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 3, 26, 15, 30, 0, DateTimeKind.Utc));
        var handler = new GetUserAiUsageSummaryQueryHandler(userRepository, aiUsageRepository, dateTimeProvider);

        var result = await handler.Handle(new GetUserAiUsageSummaryQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), aiUsageRepository.LastFromUtc);
        Assert.Equal(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), aiUsageRepository.LastToUtc);
        Assert.Equal(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), result.Value.ResetAtUtc);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryQueryHandler_WithInactiveUser_ReturnsInvalidToken() {
        var user = User.Create("inactive-ai@example.com", "hash");
        user.Deactivate();
        var handler = new GetUserAiUsageSummaryQueryHandler(
            new StubUserRepository(user),
            new RecordingAiUsageRepository(),
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 15, 30, 0, DateTimeKind.Utc)));

        var result = await handler.Handle(new GetUserAiUsageSummaryQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User? user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult(user is not null && user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult(user is not null && user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubImageAssetRepository(ImageAsset? asset = null) : IImageAssetRepository {
        public Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(asset is not null && asset.Id == id ? asset : null);

        public Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ImageAsset>> GetUnusedOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubOpenAiFoodService : IOpenAiFoodService {
        public bool WasAnalyzeFoodImageCalled { get; private set; }
        public bool WasParseFoodTextCalled { get; private set; }
        public bool WasCalculateNutritionCalled { get; private set; }
        public string? LastImageUrl { get; private set; }
        public string? LastText { get; private set; }
        public string? LastLanguage { get; private set; }
        public string? LastDescription { get; private set; }

        public Task<Result<FoodVisionModel>> AnalyzeFoodImageAsync(
            string imageUrl,
            string? userLanguage,
            UserId userId,
            string? description,
            CancellationToken cancellationToken) {
            WasAnalyzeFoodImageCalled = true;
            LastImageUrl = imageUrl;
            LastLanguage = userLanguage;
            LastDescription = description;
            return Task.FromResult(Result.Success(new FoodVisionModel(
                [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)],
                null)));
        }

        public Task<Result<FoodVisionModel>> ParseFoodTextAsync(
            string text,
            string? userLanguage,
            UserId userId,
            CancellationToken cancellationToken) {
            WasParseFoodTextCalled = true;
            LastText = text;
            LastLanguage = userLanguage;
            return Task.FromResult(Result.Success(new FoodVisionModel(
                [new FoodVisionItemModel("apple", "apple", 120, "g", 0.95m)],
                null)));
        }

        public Task<Result<FoodNutritionModel>> CalculateNutritionAsync(
            IReadOnlyList<FoodVisionItemModel> items,
            UserId userId,
            CancellationToken cancellationToken) {
            WasCalculateNutritionCalled = true;
            return Task.FromResult(Result.Success(new FoodNutritionModel(
                52,
                0,
                0,
                14,
                2,
                0,
                [new FoodNutritionItemModel("apple", 120, "g", 52, 0, 0, 14, 2, 0)])));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubImageStorageService(bool isValid = true, string? message = null) : IImageStorageService {
        public Task<PresignedUpload> CreatePresignedUploadAsync(
            UserId userId,
            string fileName,
            string contentType,
            long fileSizeBytes,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(
            string objectKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ImageObjectValidationResult(isValid, Message: message));
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingAiUsageRepository : IAiUsageRepository {
        public DateTime LastFromUtc { get; private set; }
        public DateTime LastToUtc { get; private set; }

        public Task AddAsync(FoodDiary.Domain.Entities.Ai.AiUsage usage, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<FoodDiary.Application.Abstractions.Admin.Models.AiUsageSummary> GetSummaryAsync(
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AiUsageTotals> GetUserTotalsAsync(
            UserId userId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) {
            LastFromUtc = fromUtc;
            LastToUtc = toUtc;
            return Task.FromResult(new AiUsageTotals(12, 34));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow => utcNow;
    }
}
