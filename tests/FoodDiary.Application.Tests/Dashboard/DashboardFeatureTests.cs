using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
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

        var result = await validator.ValidateAsync(query);

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

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DashboardMapping_ToWeeklyCalories_OrdersByDateAscending() {
        var day1 = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var day2 = day1.AddDays(1);
        var responses = new List<AggregatedStatisticsModel> {
            new(day2, day2, 2000, 100, 70, 250, 30),
            new(day1, day1, 1800, 90, 60, 220, 25)
        };

        var calories = DashboardMapping.ToWeeklyCalories(responses);

        Assert.Collection(
            calories,
            c => Assert.Equal(day1, c.Date),
            c => Assert.Equal(day2, c.Date));
    }

    [Fact]
    public void DashboardMapping_ToWeightModel_MapsLatestAndPreviousEntries() {
        var userId = UserId.New();
        var latestDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        var previousDate = latestDate.AddDays(-1);
        var entries = new List<WeightEntry> {
            WeightEntry.Create(userId, latestDate, 82.5),
            WeightEntry.Create(userId, previousDate, 83)
        };

        var dto = DashboardMapping.ToWeightModel(entries, desired: 80);

        Assert.NotNull(dto.Latest);
        Assert.NotNull(dto.Previous);
        Assert.Equal(82.5, dto.Latest!.Weight);
        Assert.Equal(83, dto.Previous!.Weight);
        Assert.Equal(80, dto.Desired);
    }

    [Fact]
    public void DashboardMapping_ToWaistModel_MapsLatestAndPreviousEntries() {
        var userId = UserId.New();
        var latestDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        var previousDate = latestDate.AddDays(-1);
        var entries = new List<WaistEntry> {
            WaistEntry.Create(userId, latestDate, 92.1),
            WaistEntry.Create(userId, previousDate, 92.8)
        };

        var dto = DashboardMapping.ToWaistModel(entries, desired: 90);

        Assert.NotNull(dto.Latest);
        Assert.NotNull(dto.Previous);
        Assert.Equal(92.1, dto.Latest!.Circumference);
        Assert.Equal(92.8, dto.Previous!.Circumference);
        Assert.Equal(90, dto.Desired);
    }

    [Fact]
    public async Task GetDashboardSnapshotQueryHandler_ForwardsRequestToBuilder() {
        var userId = UserId.New();
        var date = new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc);
        var builder = new RecordingDashboardSnapshotBuilder();
        var handler = new GetDashboardSnapshotQueryHandler(builder);
        using var cts = new CancellationTokenSource();

        var result = await handler.Handle(
            new GetDashboardSnapshotQuery(userId.Value, date, Page: 2, PageSize: 25, Locale: "ru", TrendDays: 14),
            cts.Token);

        Assert.True(result.IsSuccess);
        Assert.NotNull(builder.LastRequest);
        Assert.Equal(userId.Value, builder.LastRequest.UserId);
        Assert.Equal(date, builder.LastRequest.Date);
        Assert.Equal("ru", builder.LastRequest.Locale);
        Assert.Equal(14, builder.LastRequest.TrendDays);
        Assert.Equal(2, builder.LastRequest.Page);
        Assert.Equal(25, builder.LastRequest.PageSize);
        Assert.Equal(cts.Token, builder.LastCancellationToken);
    }

    [Fact]
    public async Task SendDashboardTestEmail_WhenEmailSenderFails_ReturnsValidationFailure() {
        var user = User.Create("dashboard-email@example.com", "hash");
        var handler = new SendDashboardTestEmailCommandHandler(
            new SingleUserRepository(user),
            new RecordingEmailSender(throwOnSend: true),
            NullLogger<SendDashboardTestEmailCommandHandler>.Instance);

        var result = await handler.Handle(new SendDashboardTestEmailCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("TestEmail", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDashboardTestEmail_WithAccessibleUser_SendsToUserEmailAndLanguage() {
        var user = User.Create("dashboard-email-ok@example.com", "hash");
        user.SetLanguage("ru");
        var emailSender = new RecordingEmailSender();
        var handler = new SendDashboardTestEmailCommandHandler(
            new SingleUserRepository(user),
            emailSender,
            NullLogger<SendDashboardTestEmailCommandHandler>.Instance);

        var result = await handler.Handle(new SendDashboardTestEmailCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("dashboard-email-ok@example.com", emailSender.TestEmailMessage?.ToEmail);
        Assert.Equal("ru", emailSender.TestEmailMessage?.Language);
    }

    private sealed class RecordingDashboardSnapshotBuilder : IDashboardSnapshotBuilder {
        public DashboardSnapshotRequest? LastRequest { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<Result<DashboardSnapshotModel>> BuildAsync(
            DashboardSnapshotRequest request,
            CancellationToken cancellationToken = default) {
            LastRequest = request;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Result.Success<DashboardSnapshotModel>(null!));
        }
    }

    private sealed class RecordingEmailSender(bool throwOnSend = false) : IEmailSender {
        public TestEmailMessage? TestEmailMessage { get; private set; }

        public Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendTestEmailAsync(TestEmailMessage message, CancellationToken cancellationToken) {
            if (throwOnSend) {
                throw new InvalidOperationException("send failed");
            }

            TestEmailMessage = message;
            return Task.CompletedTask;
        }
    }

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
