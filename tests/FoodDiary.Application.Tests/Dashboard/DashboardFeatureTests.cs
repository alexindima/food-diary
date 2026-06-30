using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Dashboard.Commands.SendDashboardTestEmail;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Application.Tests.Dashboard;

[ExcludeFromCodeCoverage]
public class DashboardFeatureTests {
    [Fact]
    public async Task GetDashboardSnapshotQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetDashboardSnapshotQueryValidator();
        var query = new GetDashboardSnapshotQuery(
            UserId.Empty,
            DateTime.UtcNow,
            Page: 1,
            PageSize: 10,
            Locale: "en",
            TrendDays: 7);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetDashboardSnapshotQueryValidator_WithValidInput_Passes() {
        var validator = new GetDashboardSnapshotQueryValidator();
        var query = new GetDashboardSnapshotQuery(
            UserId.New(),
            DateTime.UtcNow,
            Page: 1,
            PageSize: 10,
            Locale: "en",
            TrendDays: 7);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task SendDashboardTestEmailCommandValidator_WithEmptyUserId_Fails() {
        var validator = new SendDashboardTestEmailCommandValidator();

        ValidationResult result = await validator.ValidateAsync(new SendDashboardTestEmailCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => string.Equals(error.ErrorCode, "Authentication.InvalidToken", StringComparison.Ordinal));
    }

    [Fact]
    public void DashboardMapping_ToStatisticsModel_WhenResponseIsNull_ReturnsEmptyModel() {
        DashboardStatisticsModel dto = DashboardMapping.ToStatisticsModel((AggregatedStatisticsModel?)null, user: null);

        Assert.Equal(0, dto.TotalCalories);
        Assert.Equal(0, dto.AverageProteins);
        Assert.Null(dto.ProteinGoal);
        Assert.Null(dto.FiberGoal);
    }

    [Fact]
    public void DashboardMapping_ToStatisticsModel_MapsMacroTargetsFromUser() {
        var user = User.Create("dashboard-stats@example.com", "hash");
        user.UpdateGoals(proteinTarget: 120, fatTarget: 70, carbTarget: 210, fiberTarget: 30);
        var response = new AggregatedStatisticsModel(
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            1900,
            110,
            65,
            200,
            28);

        DashboardStatisticsModel dto = DashboardMapping.ToStatisticsModel(response, user);

        Assert.Equal(1900, dto.TotalCalories);
        Assert.Equal(110, dto.AverageProteins);
        Assert.Equal(120, dto.ProteinGoal);
        Assert.Equal(70, dto.FatGoal);
        Assert.Equal(210, dto.CarbGoal);
        Assert.Equal(30, dto.FiberGoal);
    }

    [Fact]
    public void DashboardMapping_ToWeeklyCalories_OrdersByDateAscending() {
        var day1 = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime day2 = day1.AddDays(1);
        var responses = new List<AggregatedStatisticsModel> {
            new(day2, day2, 2000, 100, 70, 250, 30),
            new(day1, day1, 1800, 90, 60, 220, 25),
        };

        IReadOnlyList<DailyCaloriesModel> calories = DashboardMapping.ToWeeklyCalories(responses);

        Assert.Collection(
            calories,
            c => Assert.Equal(day1, c.Date),
            c => Assert.Equal(day2, c.Date));
    }

    [Fact]
    public void DashboardMapping_ToWeightModel_MapsLatestAndPreviousEntries() {
        var userId = UserId.New();
        var latestDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        DateTime previousDate = latestDate.AddDays(-1);
        var entries = new List<WeightEntry> {
            WeightEntry.Create(userId, latestDate, 82.5),
            WeightEntry.Create(userId, previousDate, 83),
        };

        DashboardWeightModel dto = DashboardMapping.ToWeightModel(entries, desired: 80);

        Assert.NotNull(dto.Latest);
        Assert.NotNull(dto.Previous);
        Assert.Equal(82.5, dto.Latest!.Weight);
        Assert.Equal(83, dto.Previous!.Weight);
        Assert.Equal(80, dto.Desired);
    }

    [Fact]
    public void DashboardMapping_ToWeightModel_WithNoEntries_ReturnsEmptyPoints() {
        DashboardWeightModel dto = DashboardMapping.ToWeightModel(Array.Empty<WeightEntry>(), desired: null);

        Assert.Null(dto.Latest);
        Assert.Null(dto.Previous);
        Assert.Null(dto.Desired);
    }

    [Fact]
    public void DashboardMapping_ToWaistModel_MapsLatestAndPreviousEntries() {
        var userId = UserId.New();
        var latestDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        DateTime previousDate = latestDate.AddDays(-1);
        var entries = new List<WaistEntry> {
            WaistEntry.Create(userId, latestDate, 92.1),
            WaistEntry.Create(userId, previousDate, 92.8),
        };

        DashboardWaistModel dto = DashboardMapping.ToWaistModel(entries, desired: 90);

        Assert.NotNull(dto.Latest);
        Assert.NotNull(dto.Previous);
        Assert.Equal(92.1, dto.Latest!.Circumference);
        Assert.Equal(92.8, dto.Previous!.Circumference);
        Assert.Equal(90, dto.Desired);
    }

    [Fact]
    public void DashboardMapping_ToWaistModel_WithNoEntries_ReturnsEmptyPoints() {
        DashboardWaistModel dto = DashboardMapping.ToWaistModel(Array.Empty<WaistEntry>(), desired: null);

        Assert.Null(dto.Latest);
        Assert.Null(dto.Previous);
        Assert.Null(dto.Desired);
    }

    [Fact]
    public async Task GetDashboardSnapshotQueryHandler_ForwardsRequestToBuilder() {
        var userId = UserId.New();
        var date = new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc);
        IDashboardSnapshotBuilder builder = CreateDashboardSnapshotBuilder(
            out Func<DashboardSnapshotRequest?> getLastRequest,
            out Func<CancellationToken> getLastCancellationToken);
        GetDashboardSnapshotQueryHandler handler = new(builder);
        using var cts = new CancellationTokenSource();

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetDashboardSnapshotQuery(userId.Value, date, Page: 2, PageSize: 25, Locale: "ru", TrendDays: 14),
            cts.Token);

        ResultAssert.Success(result);
        DashboardSnapshotRequest request = Assert.IsType<DashboardSnapshotRequest>(getLastRequest());
        Assert.Equal(userId.Value, request.UserId);
        Assert.Equal(date, request.Date);
        Assert.Equal("ru", request.Locale);
        Assert.Equal(14, request.TrendDays);
        Assert.Equal(2, request.Page);
        Assert.Equal(25, request.PageSize);
        Assert.Equal(cts.Token, getLastCancellationToken());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task GetDashboardSnapshotQueryHandler_WithMissingUserId_ReturnsInvalidToken(string? userIdText) {
        IDashboardSnapshotBuilder builder = CreateDashboardSnapshotBuilder(out Func<DashboardSnapshotRequest?> getLastRequest, out _);
        GetDashboardSnapshotQueryHandler handler = new(builder);
        Guid? userId = userIdText is null ? (Guid?)null : Guid.Parse(userIdText);

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetDashboardSnapshotQuery(userId, DateTime.UtcNow, Page: 1, PageSize: 10, Locale: "en", TrendDays: 7),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Null(getLastRequest());
    }

    [Fact]
    public async Task SendDashboardTestEmail_WhenEmailSenderFails_ReturnsValidationFailure() {
        var user = User.Create("dashboard-email@example.com", "hash");
        var handler = new SendDashboardTestEmailCommandHandler(
            CreateUserRepository(user),
            CreateThrowingEmailSender(),
            NullLogger<SendDashboardTestEmailCommandHandler>.Instance);

        Result result = await handler.Handle(new SendDashboardTestEmailCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("TestEmail", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDashboardTestEmail_WithAccessibleUser_SendsToUserEmailAndLanguage() {
        var user = User.Create("dashboard-email-ok@example.com", "hash");
        user.SetLanguage("ru");
        IEmailSender emailSender = CreateEmailSender(out Func<TestEmailMessage?> getLastMessage);
        var handler = new SendDashboardTestEmailCommandHandler(
            CreateUserRepository(user),
            emailSender,
            NullLogger<SendDashboardTestEmailCommandHandler>.Instance);

        Result result = await handler.Handle(new SendDashboardTestEmailCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        TestEmailMessage message = Assert.IsType<TestEmailMessage>(getLastMessage());
        Assert.Equal("dashboard-email-ok@example.com", message.ToEmail);
        Assert.Equal("ru", message.Language);
    }

    [Fact]
    public async Task SendDashboardTestEmail_WhenUserMissing_ReturnsInvalidToken() {
        IEmailSender emailSender = CreateEmailSender(out Func<TestEmailMessage?> getLastMessage);
        var handler = new SendDashboardTestEmailCommandHandler(
            CreateUserRepository(user: null),
            emailSender,
            NullLogger<SendDashboardTestEmailCommandHandler>.Instance);

        Result result = await handler.Handle(new SendDashboardTestEmailCommand(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Null(getLastMessage());
    }

    private static IDashboardSnapshotBuilder CreateDashboardSnapshotBuilder(
        out Func<DashboardSnapshotRequest?> getLastRequest,
        out Func<CancellationToken> getLastCancellationToken) {
        IDashboardSnapshotBuilder builder = Substitute.For<IDashboardSnapshotBuilder>();
        DashboardSnapshotRequest? lastRequest = null;
        CancellationToken lastCancellationToken = default;
        builder
            .BuildAsync(
                Arg.Do<DashboardSnapshotRequest>(request => lastRequest = request),
                Arg.Do<CancellationToken>(cancellationToken => lastCancellationToken = cancellationToken))
            .Returns(Task.FromResult(Result.Success<DashboardSnapshotModel>(null!)));
        getLastRequest = () => lastRequest;
        getLastCancellationToken = () => lastCancellationToken;
        return builder;
    }

    private static IEmailSender CreateEmailSender(out Func<TestEmailMessage?> getLastMessage) {
        IEmailSender emailSender = Substitute.For<IEmailSender>();
        TestEmailMessage? lastMessage = null;
        emailSender
            .SendTestEmailAsync(Arg.Do<TestEmailMessage>(message => lastMessage = message), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        getLastMessage = () => lastMessage;
        return emailSender;
    }

    private static IEmailSender CreateThrowingEmailSender() {
        IEmailSender emailSender = Substitute.For<IEmailSender>();
        emailSender
            .SendTestEmailAsync(Arg.Any<TestEmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("send failed")));
        return emailSender;
    }

    private static IUserRepository CreateUserRepository(User? user) {
        IUserRepository repository = Substitute.For<IUserRepository>();
        repository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user is not null && user.Id == id ? user : null);
            });
        repository
            .GetByIdIncludingDeletedAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user is not null && user.Id == id ? user : null);
            });
        return repository;
    }
}
