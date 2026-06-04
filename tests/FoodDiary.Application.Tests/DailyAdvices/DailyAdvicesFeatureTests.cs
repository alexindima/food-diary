using System.Reflection;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.DailyAdvices;

[ExcludeFromCodeCoverage]
public class DailyAdvicesFeatureTests {
    [Fact]
    public async Task GetDailyAdviceQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetDailyAdviceQueryValidator();
        var query = new GetDailyAdviceQuery(Guid.Empty, DateTime.UtcNow, "en");

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetDailyAdviceQueryValidator_WithValidInput_Passes() {
        var validator = new GetDailyAdviceQueryValidator();
        var query = new GetDailyAdviceQuery(Guid.NewGuid(), DateTime.UtcNow, "ru-RU");

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DailyAdviceSelector_NormalizeLocale_UnsupportedLocaleFallsBackToEn() {
        var normalized = InvokeNormalizeLocale("de-DE");

        Assert.Equal("en", normalized);
    }

    [Fact]
    public void DailyAdviceSelector_SelectForDate_ReturnsAdviceFromRequestedLocale() {
        var advices = new List<DailyAdvice> {
            DailyAdvice.Create("Hydrate", "en", weight: 1),
            DailyAdvice.Create("Walk", "en", weight: 1),
            DailyAdvice.Create("ÐŸÐµÐ¹ Ð²Ð¾Ð´Ñƒ", "ru", weight: 1)
        };

        var date = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        var selected = InvokeSelectForDate(advices, date, "ru-RU");

        Assert.NotNull(selected);
        Assert.Equal("ru", selected!.Locale);
    }

    [Fact]
    public void DailyAdviceSelector_SelectForDate_WhenLocaleHasNoAdvice_ReturnsNull() {
        var advices = new List<DailyAdvice> {
            DailyAdvice.Create("Hydrate", "en", weight: 1)
        };

        var selected = InvokeSelectForDate(advices, DateTime.UtcNow, "ru");

        Assert.Null(selected);
    }

    [Fact]
    public void DailyAdviceSelector_SelectForDate_WithEmptyAdviceList_ReturnsNull() {
        var selected = InvokeSelectForDate([], DateTime.UtcNow, "en");

        Assert.Null(selected);
    }

    [Fact]
    public void DailyAdviceSelector_SelectForDate_WhenPreviousDaySelectsSameAdvice_UsesNextAdvice() {
        var advices = new List<DailyAdvice> {
            DailyAdvice.Create("Hydrate", "en", weight: 1),
            DailyAdvice.Create("Walk", "en", weight: 1),
            DailyAdvice.Create("Sleep", "en", weight: 1)
        };
        var ordered = advices.OrderBy(advice => advice.Id.Value).ToList();
        var date = Enumerable
            .Range(0, 10_000)
            .Select(offset => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(offset))
            .First(candidate =>
                InvokeGetWeightedIndex(ordered, candidate, "en") ==
                InvokeGetWeightedIndex(ordered, candidate.AddDays(-1), "en"));
        var todayIndex = InvokeGetWeightedIndex(ordered, date, "en");

        var selected = InvokeSelectForDate(advices, date, "en");

        Assert.NotNull(selected);
        Assert.Equal(ordered[(todayIndex + 1) % ordered.Count].Id, selected!.Id);
    }

    [Fact]
    public async Task GetDailyAdvice_WithInvalidUserId_ReturnsInvalidToken() {
        var handler = new GetDailyAdviceQueryHandler(new RecordingDailyAdviceRepository(), new SingleUserRepository(User.Create("advice@example.com", "hash")));

        var result = await handler.Handle(new GetDailyAdviceQuery(Guid.Empty, DateTime.UtcNow, "en"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetDailyAdvice_WhenUserDeleted_ReturnsAccountDeleted() {
        var user = User.Create("deleted-advice@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetDailyAdviceQueryHandler(new RecordingDailyAdviceRepository(), new SingleUserRepository(user));

        var result = await handler.Handle(new GetDailyAdviceQuery(user.Id.Value, DateTime.UtcNow, "en"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetDailyAdvice_WithUnsupportedLocale_FallsBackToEnglishAdvice() {
        var user = User.Create("fallback-advice@example.com", "hash");
        var repository = new RecordingDailyAdviceRepository();
        repository.Seed("en", DailyAdvice.Create("Hydrate", "en", weight: 1));
        var handler = new GetDailyAdviceQueryHandler(repository, new SingleUserRepository(user));

        var result = await handler.Handle(new GetDailyAdviceQuery(user.Id.Value, DateTime.UtcNow, "de-DE"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("en", result.Value.Locale);
        Assert.Equal(["en"], repository.RequestedLocales);
    }

    [Fact]
    public async Task GetDailyAdvice_WhenNoAdviceExists_ReturnsNotFound() {
        var user = User.Create("missing-advice@example.com", "hash");
        var handler = new GetDailyAdviceQueryHandler(new RecordingDailyAdviceRepository(), new SingleUserRepository(user));

        var result = await handler.Handle(new GetDailyAdviceQuery(user.Id.Value, DateTime.UtcNow, "ru"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("DailyAdvice.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetDailyAdvice_WhenLoadedAdviceDoesNotMatchLocale_ReturnsNotFound() {
        var user = User.Create("locale-mismatch-advice@example.com", "hash");
        var repository = new RecordingDailyAdviceRepository();
        repository.Seed("en", DailyAdvice.Create("Russian advice", "ru", weight: 1));
        var handler = new GetDailyAdviceQueryHandler(repository, new SingleUserRepository(user));

        var result = await handler.Handle(new GetDailyAdviceQuery(user.Id.Value, DateTime.UtcNow, "en"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("DailyAdvice.NotFound", result.Error.Code);
    }

    private static string InvokeNormalizeLocale(string locale) {
        var selectorType = GetSelectorType();
        var method = selectorType.GetMethod("NormalizeLocale", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);

        return (string)method!.Invoke(null, [locale])!;
    }

    private static DailyAdvice? InvokeSelectForDate(IReadOnlyList<DailyAdvice> advices, DateTime date, string locale) {
        var selectorType = GetSelectorType();
        var method = selectorType.GetMethod("SelectForDate", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(method);

        return (DailyAdvice?)method!.Invoke(null, [advices, date, locale]);
    }

    private static int InvokeGetWeightedIndex(IReadOnlyList<DailyAdvice> advices, DateTime date, string locale) {
        var selectorType = GetSelectorType();
        var method = selectorType.GetMethod("GetWeightedIndex", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        return (int)method!.Invoke(null, [advices, date, locale])!;
    }

    private static Type GetSelectorType() {
        var selectorType = Type.GetType("FoodDiary.Application.DailyAdvices.Services.DailyAdviceSelector, FoodDiary.Application");
        Assert.NotNull(selectorType);
        return selectorType!;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingDailyAdviceRepository : IDailyAdviceRepository {
        private readonly Dictionary<string, IReadOnlyList<DailyAdvice>> _advices = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> RequestedLocales => _requestedLocales;

        private readonly List<string> _requestedLocales = [];

        public void Seed(string locale, params DailyAdvice[] advices) {
            _advices[locale] = advices;
        }

        public Task<IReadOnlyList<DailyAdvice>> GetByLocaleAsync(
            string locale,
            CancellationToken cancellationToken = default) {
            _requestedLocales.Add(locale);
            return Task.FromResult(_advices.GetValueOrDefault(locale, []));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleUserRepository(User user) : IUserRepository {
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
}
