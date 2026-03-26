using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Models;
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

    private sealed class RecordingAiUsageRepository : IAiUsageRepository {
        public DateTime LastFromUtc { get; private set; }
        public DateTime LastToUtc { get; private set; }

        public Task AddAsync(FoodDiary.Domain.Entities.Ai.AiUsage usage, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<FoodDiary.Application.Admin.Models.AiUsageSummary> GetSummaryAsync(
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
