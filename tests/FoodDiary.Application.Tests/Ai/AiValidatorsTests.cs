using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
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
            new StubOpenAiFoodService());

        var result = await handler.Handle(
            new AnalyzeFoodImageCommand(user.Id.Value, Guid.Empty, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
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
        var handler = new CalculateFoodNutritionCommandHandler(new StubOpenAiFoodService());

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

    private sealed class StubUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubImageAssetRepository : IImageAssetRepository {
        public Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ImageAsset?>(null);

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

    private sealed class StubOpenAiFoodService : IOpenAiFoodService {
        public Task<Result<FoodVisionModel>> AnalyzeFoodImageAsync(
            string imageUrl,
            string? userLanguage,
            UserId userId,
            string? description,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<FoodVisionModel>> ParseFoodTextAsync(
            string text,
            string? userLanguage,
            UserId userId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<FoodNutritionModel>> CalculateNutritionAsync(
            IReadOnlyList<FoodVisionItemModel> items,
            UserId userId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

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

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow => utcNow;
    }
}
