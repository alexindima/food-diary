using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;
using System.Reflection;
using FluentValidation.Results;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Application.Queries.GetDailyAdvice;
using FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdvice;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.DailyAdvices.Application.Tests;

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
    public async Task GetDailyAdviceQueryValidator_WithFirstCalendarDay_PreservesSupportedInput() {
        var validator = new GetDailyAdviceQueryValidator();
        var date = new DateTime(1, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var query = new GetDailyAdviceQuery(Guid.NewGuid(), date, "en");

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DailyAdviceSelector_NormalizeLocale_UnsupportedLocaleFallsBackToEn() {
        string normalized = InvokeNormalizeLocale("de-DE");

        Assert.Equal("en", normalized);
    }

    [Fact]
    public void DailyAdviceSelector_SelectReadModelForDate_ReturnsAdviceFromRequestedLocale() {
        var advices = new List<DailyAdviceReadModel> {
            new(Guid.NewGuid(), "en", "Advice", Tag: null, 1),
            new(Guid.NewGuid(), "en", "Advice", Tag: null, 1),
            new(Guid.NewGuid(), "ru", "Пей воду", Tag: null, 1),
        };

        var date = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        DailyAdviceReadModel? selected = InvokeSelectReadModelForDate(advices, date, "ru-RU");

        Assert.NotNull(selected);
        Assert.Equal("ru", selected!.Locale);
    }

    [Fact]
    public void DailyAdviceSelector_SelectReadModelForDate_WhenLocaleHasNoAdvice_ReturnsNull() {
        var advices = new List<DailyAdviceReadModel> {
            new(Guid.NewGuid(), "en", "Advice", Tag: null, 1),
        };

        DailyAdviceReadModel? selected = InvokeSelectReadModelForDate(advices, DateTime.UtcNow, "ru");

        Assert.Null(selected);
    }

    [Fact]
    public void DailyAdviceSelector_SelectReadModelForDate_WithEmptyAdviceList_ReturnsNull() {
        DailyAdviceReadModel? selected = InvokeSelectReadModelForDate([], DateTime.UtcNow, "en");

        Assert.Null(selected);
    }

    [Fact]
    public void DailyAdviceSelector_ConsecutiveCorrectedDays_DoNotRepeat() {
        IReadOnlyList<DailyAdviceReadModel> advices = CreateSelectionAdvices(3, 1);
        var date = new DateTime(2026, 1, 4);

        DailyAdviceReadModel? first = InvokeSelectReadModelForDate(advices, date, "en");
        DailyAdviceReadModel? second = InvokeSelectReadModelForDate(advices, date.AddDays(1), "en");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(7, 1)]
    [InlineData(2, int.MaxValue)]
    [InlineData(3, int.MaxValue)]
    [InlineData(7, int.MaxValue)]
    public void DailyAdviceSelector_StableCatalog_DoesNotRepeatAcrossCalendarDays(int count, int weight) {
        IReadOnlyList<DailyAdviceReadModel> advices = CreateSelectionAdvices(count, weight);
        var start = new DateTime(2025, 12, 1);
        Guid? previous = null;
        for (int day = 0; day < 400; day++) {
            DailyAdviceReadModel? selected = InvokeSelectReadModelForDate(advices, start.AddDays(day), "en");
            Assert.NotNull(selected);
            Assert.Contains(selected, advices);
            Assert.NotEqual(previous, selected.Id);
            previous = selected.Id;
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    public void DailyAdviceSelector_CalendarBoundaries_DoNotOverflowOrRepeat(int count) {
        IReadOnlyList<DailyAdviceReadModel> advices = CreateSelectionAdvices(count, int.MaxValue);
        foreach (DateTime date in new[] { DateTime.MinValue, DateTime.MaxValue.Date.AddDays(-1) }) {
            DailyAdviceReadModel? first = InvokeSelectReadModelForDate(advices, date, "en");
            DailyAdviceReadModel? second = InvokeSelectReadModelForDate(advices, date.AddDays(1), "en");
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.NotEqual(first.Id, second.Id);
        }
    }

    [Fact]
    public void DailyAdviceSelector_SelectionIsIndependentOfInputAndRequestOrder() {
        IReadOnlyList<DailyAdviceReadModel> advices = CreateSelectionAdvices(7, int.MaxValue);
        var date = new DateTime(2026, 1, 5);
        DailyAdviceReadModel? expected = InvokeSelectReadModelForDate(advices, date, "en");
        _ = InvokeSelectReadModelForDate(advices, date.AddYears(10), "en");
        DailyAdviceReadModel? actual = InvokeSelectReadModelForDate([.. advices.Reverse()], date.AddHours(12), "en-US");

        Assert.NotNull(expected);
        Assert.Equal(expected, actual);
    }

    private static IReadOnlyList<DailyAdviceReadModel> CreateSelectionAdvices(int count, int weight) =>
        [.. Enumerable.Range(1, count).Select(index =>
            new DailyAdviceReadModel(new Guid(index, 0, 0, new byte[8]), "en", "Advice", Tag: null, index % 2 == 0 ? 1 : weight))];

    [Fact]
    public void DailyAdviceSelector_SelectReadModelForDate_WithMinimumDateAndMultipleAdvices_ReturnsSelection() {
        var advices = new List<DailyAdviceReadModel> {
            new(Guid.NewGuid(), "en", "Advice", Tag: null, 1),
            new(Guid.NewGuid(), "en", "Advice", Tag: null, 1),
        };

        DailyAdviceReadModel? selected = InvokeSelectReadModelForDate(advices, DateTime.MinValue, "en");

        Assert.NotNull(selected);
        Assert.Contains(selected, advices);
    }

    [Fact]
    public void DailyAdviceSelector_SelectReadModelForDate_WithMinimumCalendarDayAndMultipleAdvices_ReturnsSelection() {
        IReadOnlyList<DailyAdviceReadModel> advices = [
            new DailyAdviceReadModel(Guid.Parse("11111111-1111-1111-1111-111111111111"), "en", "Hydrate", "water", 1),
            new DailyAdviceReadModel(Guid.Parse("22222222-2222-2222-2222-222222222222"), "en", "Walk", "movement", 1),
        ];
        var date = new DateTime(1, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

        DailyAdviceReadModel? selected = InvokeSelectReadModelForDate(advices, date, "en");

        Assert.NotNull(selected);
        Assert.Contains(selected, advices);
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
        IDailyAdviceReadModelRepository repository = CreateDailyAdviceRepository(
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
    public async Task GetDailyAdvice_WithMinimumCalendarDayAndMultipleAdvices_ReturnsAdvice() {
        var user = User.Create("minimum-date-advice@example.com", "hash");
        IDailyAdviceReadModelRepository repository = CreateDailyAdviceRepository(
            new Dictionary<string, IReadOnlyList<DailyAdvice>>(StringComparer.OrdinalIgnoreCase) {
                ["en"] = [
                    DailyAdvice.Create("Hydrate", "en", weight: 1),
                    DailyAdvice.Create("Walk", "en", weight: 1),
                ],
            });
        GetDailyAdviceQueryHandler handler = CreateGetDailyAdviceHandler(repository, CreateCurrentUserAccessService(user));
        var date = new DateTime(1, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

        Result<DailyAdviceModel> result = await handler.Handle(
            new GetDailyAdviceQuery(user.Id.Value, date, "en"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Contains(result.Value.Value, new[] { "Hydrate", "Walk" }, StringComparer.Ordinal);
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
        IDailyAdviceReadModelRepository repository = CreateDailyAdviceRepository(
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

    private static DailyAdviceReadModel? InvokeSelectReadModelForDate(IReadOnlyList<DailyAdviceReadModel> advices, DateTime date, string locale) {
        Type selectorType = GetSelectorType();
        MethodInfo? method = selectorType.GetMethod("SelectReadModelForDate", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(method);

        return (DailyAdviceReadModel?)method!.Invoke(null, [advices, date, locale]);
    }

    private static Type GetSelectorType() {
        var selectorType = Type.GetType("FoodDiary.Modules.DailyAdvices.Application.Services.DailyAdviceSelector, FoodDiary.Modules.DailyAdvices.Application");
        Assert.NotNull(selectorType);
        return selectorType!;
    }

    private static IDailyAdviceReadModelRepository CreateDailyAdviceRepository() =>
        CreateDailyAdviceRepository(new Dictionary<string, IReadOnlyList<DailyAdvice>>(StringComparer.OrdinalIgnoreCase), out _);

    private static IDailyAdviceReadModelRepository CreateDailyAdviceRepository(
        IReadOnlyDictionary<string, IReadOnlyList<DailyAdvice>> advices) =>
        CreateDailyAdviceRepository(advices, out _);

    private static IDailyAdviceReadModelRepository CreateDailyAdviceRepository(
        IReadOnlyDictionary<string, IReadOnlyList<DailyAdvice>> advices,
        out List<string> requestedLocales) {
        List<string> capturedRequestedLocales = [];
        IDailyAdviceReadModelRepository repository = Substitute.For<IDailyAdviceReadModelRepository>();
        repository
            .GetByLocaleReadModelsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                string locale = call.Arg<string>()!;
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
        new(repository, currentUserAccessService);

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        Error? error = user.DeletedAt is null ? null : UserAuthenticationErrors.AccountDeleted;
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user.Id == id ? error : AuthenticationErrors.InvalidToken);
            });
        return service;
    }
}
