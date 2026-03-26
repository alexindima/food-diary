using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Models;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class OpenAiFoodServiceTests {
    [Fact]
    public async Task CalculateNutritionAsync_WhenTransportFails_ReturnsOpenAiFailedError() {
        using var httpClient = new HttpClient(new ThrowingHttpMessageHandler(new HttpRequestException("boom")));
        var service = new OpenAiFoodService(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new OpenAiOptions { ApiKey = "test-key", TextModel = "test-model" }),
            NullLogger<OpenAiFoodService>.Instance,
            new StubAiUsageRepository(),
            new StubUserRepository());

        var result = await service.CalculateNutritionAsync(
            [new FoodVisionItemModel("Apple", null, 100m, "g", 0.9m)],
            UserId.New(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ai.OpenAiFailed", result.Error.Code);
        Assert.Contains("transport error", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }

    private sealed class StubAiUsageRepository : IAiUsageRepository {
        public Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<AiUsageSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiUsageSummary(0, 0, 0, [], [], [], []));

        public Task<AiUsageTotals> GetUserTotalsAsync(UserId userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiUsageTotals(0, 0));
    }

    private sealed class StubUserRepository : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) {
            var user = User.Create("ai-tests@example.com", "hash");
            return Task.FromResult<User?>(user);
        }
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
