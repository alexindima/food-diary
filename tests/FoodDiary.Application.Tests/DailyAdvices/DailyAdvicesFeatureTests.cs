using System.Reflection;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Application.Abstractions.DailyAdvices.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.DailyAdvices.Common;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.DailyAdvices.Services;
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

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetDailyAdviceQueryValidator_WithValidInput_Passes() {
        var validator = new GetDailyAdviceQueryValidator();
        var query = new GetDailyAdviceQuery(Guid.NewGuid(), DateTime.UtcNow, "ru-RU");

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DailyAdviceSelector_NormalizeLocale_UnsupportedLocaleFallsBackToEn() {
        string normalized = InvokeNormalizeLocale("de-DE");

        Assert.Equal("en", normalized);
    }

    [Fact]
    public void DailyAdviceSelector_SelectForDate_ReturnsAdviceFromRequestedLocale() {
        var advices = new List<DailyAdvice> {
            DailyAdvice.Create("Hydrate", "en", weight: 1),
            DailyAdvice.Create("Walk", "en", weight: 1),
            DailyAdvice.Create("ÐŸÐµÐ¹ Ð²Ð¾Ð´Ñƒ", "ru", weight: 1),
        };

        var date = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        DailyAdvice? selected = InvokeSelectForDate(advices, date, "ru-RU");

        Assert.NotNull(selected);
        Assert.Equal("ru", selected!.Locale);
    }

    [Fact]
    public void DailyAdviceSelector_SelectForDate_WhenLocaleHasNoAdvice_ReturnsNull() {
        var advices = new List<DailyAdvice> {
            DailyAdvice.Create("Hydrate", "en", weight: 1),
        };

        DailyAdvice? selected = InvokeSelectForDate(advices, DateTime.UtcNow, "ru");

        Assert.Null(selected);
    }

    [Fact]
    public void DailyAdviceSelector_SelectForDate_WithEmptyAdviceList_ReturnsNull() {
        DailyAdvice? selected = InvokeSelectForDate([], DateTime.UtcNow, "en");

        Assert.Null(selected);
    }

    [Fact]
    public void DailyAdviceSelector_SelectForDate_WhenPreviousDaySelectsSameAdvice_UsesNextAdvice() {
        var advices = new List<DailyAdvice> {
            DailyAdvice.Create("Hydrate", "en", weight: 1),
            DailyAdvice.Create("Walk", "en", weight: 1),
            DailyAdvice.Create("Sleep", "en", weight: 1),
        };
        var ordered = advices.OrderBy(advice => advice.Id.Value).ToList();
        DateTime date = Enumerable
            .Range(0, 10_000)
            .Select(offset => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(offset))
            .First(candidate =>
                InvokeGetWeightedIndex(ordered, candidate, "en") ==
                InvokeGetWeightedIndex(ordered, candidate.AddDays(-1), "en"));
        int todayIndex = InvokeGetWeightedIndex(ordered, date, "en");

        DailyAdvice? selected = InvokeSelectForDate(advices, date, "en");

        Assert.NotNull(selected);
        Assert.Equal(ordered[(todayIndex + 1) % ordered.Count].Id, selected!.Id);
    }

    [Fact]
    public async Task GetDailyAdvice_WithInvalidUserId_ReturnsInvalidToken() {
        GetDailyAdviceQueryHandler handler = CreateGetDailyAdviceHandler(CreateDailyAdviceRepository(), CreateCurrentUserAccessService(User.Create("advice@example.com", "hash")));

        Result<DailyAdviceModel> result = await handler.Handle(new GetDailyAdviceQuery(Guid.Empty, DateTime.UtcNow, "en"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetDailyAdvice_WhenUserDeleted_ReturnsAccountDeleted() {
        var user = User.Create("deleted-advice@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        GetDailyAdviceQueryHandler handler = CreateGetDailyAdviceHandler(CreateDailyAdviceRepository(), CreateCurrentUserAccessService(user));

        Result<DailyAdviceModel> result = await handler.Handle(new GetDailyAdviceQuery(user.Id.Value, DateTime.UtcNow, "en"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetDailyAdvice_WithUnsupportedLocale_FallsBackToEnglishAdvice() {
        var user = User.Create("fallback-advice@example.com", "hash");
        IDailyAdviceRepository repository = CreateDailyAdviceRepository(
            new Dictionary<string, IReadOnlyList<DailyAdvice>>(StringComparer.OrdinalIgnoreCase) {
                ["en"] = [DailyAdvice.Create("Hydrate", "en", weight: 1)],
            },
            out List<string> requestedLocales);
        GetDailyAdviceQueryHandler handler = CreateGetDailyAdviceHandler(repository, CreateCurrentUserAccessService(user));

        Result<DailyAdviceModel> result = await handler.Handle(new GetDailyAdviceQuery(user.Id.Value, DateTime.UtcNow, "de-DE"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("en", result.Value.Locale);
        Assert.Equal(["en"], requestedLocales);
    }

    [Fact]
    public async Task GetDailyAdvice_WhenNoAdviceExists_ReturnsNotFound() {
        var user = User.Create("missing-advice@example.com", "hash");
        GetDailyAdviceQueryHandler handler = CreateGetDailyAdviceHandler(CreateDailyAdviceRepository(), CreateCurrentUserAccessService(user));

        Result<DailyAdviceModel> result = await handler.Handle(new GetDailyAdviceQuery(user.Id.Value, DateTime.UtcNow, "ru"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("DailyAdvice.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetDailyAdvice_WhenLoadedAdviceDoesNotMatchLocale_ReturnsNotFound() {
        var user = User.Create("locale-mismatch-advice@example.com", "hash");
        IDailyAdviceRepository repository = CreateDailyAdviceRepository(
            new Dictionary<string, IReadOnlyList<DailyAdvice>>(StringComparer.OrdinalIgnoreCase) {
                ["en"] = [DailyAdvice.Create("Russian advice", "ru", weight: 1)],
            });
        GetDailyAdviceQueryHandler handler = CreateGetDailyAdviceHandler(repository, CreateCurrentUserAccessService(user));

        Result<DailyAdviceModel> result = await handler.Handle(new GetDailyAdviceQuery(user.Id.Value, DateTime.UtcNow, "en"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("DailyAdvice.NotFound", result.Error.Code);
    }

    private static string InvokeNormalizeLocale(string locale) {
        Type selectorType = GetSelectorType();
        MethodInfo? method = selectorType.GetMethod("NormalizeLocale", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);

        return (string)method!.Invoke(null, [locale])!;
    }

    private static DailyAdvice? InvokeSelectForDate(IReadOnlyList<DailyAdvice> advices, DateTime date, string locale) {
        Type selectorType = GetSelectorType();
        MethodInfo? method = selectorType.GetMethod("SelectForDate", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(method);

        return (DailyAdvice?)method!.Invoke(null, [advices, date, locale]);
    }

    private static int InvokeGetWeightedIndex(IReadOnlyList<DailyAdvice> advices, DateTime date, string locale) {
        Type selectorType = GetSelectorType();
        MethodInfo? method = selectorType.GetMethod("GetWeightedIndex", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        return (int)method!.Invoke(null, [advices, date, locale])!;
    }

    private static Type GetSelectorType() {
        var selectorType = Type.GetType("FoodDiary.Application.DailyAdvices.Services.DailyAdviceSelector, FoodDiary.Application");
        Assert.NotNull(selectorType);
        return selectorType!;
    }

    private static IDailyAdviceRepository CreateDailyAdviceRepository() =>
        CreateDailyAdviceRepository(new Dictionary<string, IReadOnlyList<DailyAdvice>>(StringComparer.OrdinalIgnoreCase), out _);

    private static IDailyAdviceRepository CreateDailyAdviceRepository(
        IReadOnlyDictionary<string, IReadOnlyList<DailyAdvice>> advices) =>
        CreateDailyAdviceRepository(advices, out _);

    private static IDailyAdviceRepository CreateDailyAdviceRepository(
        IReadOnlyDictionary<string, IReadOnlyList<DailyAdvice>> advices,
        out List<string> requestedLocales) {
        List<string> capturedRequestedLocales = [];
        IDailyAdviceRepository repository = Substitute.For<IDailyAdviceRepository>();
        repository
            .GetByLocaleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                string locale = call.Arg<string>();
                capturedRequestedLocales.Add(locale);
                return Task.FromResult(advices.GetValueOrDefault(locale, []));
            });
        repository
            .GetByLocaleReadModelsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                string locale = call.Arg<string>();
                capturedRequestedLocales.Add(locale);
                IReadOnlyList<DailyAdvice> items = advices.GetValueOrDefault(locale, []);
                return Task.FromResult<IReadOnlyList<DailyAdviceReadModel>>([
                    .. items.Select(static advice => new DailyAdviceReadModel(
                        advice.Id.Value,
                        advice.Locale,
                        advice.Value,
                        advice.Tag,
                        advice.Weight)),
                ]);
            });

        requestedLocales = capturedRequestedLocales;
        return repository;
    }

    private static GetDailyAdviceQueryHandler CreateGetDailyAdviceHandler(
        IDailyAdviceReadModelRepository repository,
        ICurrentUserAccessService currentUserAccessService) =>
        new(CreateDailyAdviceReadService(repository), currentUserAccessService);

    private static IDailyAdviceReadService CreateDailyAdviceReadService(
        IDailyAdviceReadModelRepository repository) =>
        new DailyAdviceReadService(repository);

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        Error? error = user.DeletedAt is null ? null : Errors.Authentication.AccountDeleted;
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user.Id == id ? error : Errors.Authentication.InvalidToken);
            });
        return service;
    }
}
