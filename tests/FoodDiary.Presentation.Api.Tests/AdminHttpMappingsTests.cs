using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class AdminHttpMappingsTests {
    [Fact]
    public void AdminUserUpdateHttpRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var request = new AdminUserUpdateHttpRequest(
            IsActive: false,
            IsEmailConfirmed: true,
            Roles: ["Admin", "User"],
            Language: "ru",
            AiInputTokenLimit: 1000,
            AiOutputTokenLimit: 2000);

        var command = request.ToCommand(userId, actorUserId);

        Assert.Equal(userId, command.UserId);
        Assert.False(command.IsActive);
        Assert.True(command.IsEmailConfirmed);
        Assert.Equal(["Admin", "User"], command.Roles);
        Assert.Equal("ru", command.Language);
        Assert.Equal(1000, command.AiInputTokenLimit);
        Assert.Equal(2000, command.AiOutputTokenLimit);
        Assert.Equal(actorUserId, command.ActorUserId);
    }

    [Fact]
    public void AdminUserUpdateHttpRequest_ToCommand_WithNullRoles_MapsEmptyRoles() {
        var request = new AdminUserUpdateHttpRequest(
            IsActive: null,
            IsEmailConfirmed: null,
            Roles: null,
            Language: null,
            AiInputTokenLimit: null,
            AiOutputTokenLimit: null);

        var command = request.ToCommand(Guid.NewGuid(), Guid.NewGuid());

        Assert.NotNull(command.Roles);
        Assert.Empty(command.Roles);
    }

    [Fact]
    public void GetAdminUsersHttpQuery_ToQuery_ParsesExplicitStatusCaseInsensitive() {
        var httpQuery = new GetAdminUsersHttpQuery(
            Page: 2,
            Limit: 30,
            Search: "alex",
            Status: "deleted",
            IncludeDeleted: false);

        var query = httpQuery.ToQuery();

        Assert.Equal(2, query.Page);
        Assert.Equal(30, query.Limit);
        Assert.Equal("alex", query.Search);
        Assert.Equal(UserAccountStatusFilter.Deleted, query.Status);
    }

    [Fact]
    public void GetAdminUsersHttpQuery_ToQuery_FallsBackToIncludeDeletedFlag() {
        var includeDeletedQuery = new GetAdminUsersHttpQuery(IncludeDeleted: true);
        var activeOnlyQuery = new GetAdminUsersHttpQuery(IncludeDeleted: false);

        Assert.Equal(UserAccountStatusFilter.All, includeDeletedQuery.ToQuery().Status);
        Assert.Equal(UserAccountStatusFilter.Active, activeOnlyQuery.ToQuery().Status);
    }

    [Theory]
    [InlineData(-10, 1)]
    [InlineData(0, 1)]
    [InlineData(7, 7)]
    [InlineData(100, 20)]
    public void GetAdminDashboardHttpQuery_ToQuery_ClampsRecentLimit(int recent, int expectedLimit) {
        var query = new GetAdminDashboardHttpQuery(recent).ToQuery();

        Assert.Equal(expectedLimit, query.RecentLimit);
    }

    [Fact]
    public void AdminLessonsImportHttpRequest_ToImportCommand_MapsNestedLessons() {
        var request = new AdminLessonsImportHttpRequest(
            Version: 3,
            Lessons: [
                new AdminLessonImportItemHttpRequest(
                    Title: "Protein basics",
                    Content: "Content",
                    Summary: null,
                    Locale: "en",
                    Category: "nutrition",
                    Difficulty: "beginner",
                    EstimatedReadMinutes: 4,
                    SortOrder: 10)
            ]);

        var command = request.ToImportCommand();

        Assert.Equal(3, command.Version);
        var lesson = Assert.Single(command.Lessons);
        Assert.Equal("Protein basics", lesson.Title);
        Assert.Equal("Content", lesson.Content);
        Assert.Null(lesson.Summary);
        Assert.Equal("en", lesson.Locale);
        Assert.Equal("nutrition", lesson.Category);
        Assert.Equal("beginner", lesson.Difficulty);
        Assert.Equal(4, lesson.EstimatedReadMinutes);
        Assert.Equal(10, lesson.SortOrder);
    }

    [Fact]
    public void AdminDashboardSummaryModel_ToHttpResponse_MapsRecentUsers() {
        var userId = Guid.NewGuid();
        var createdOnUtc = DateTime.UtcNow;
        var model = new AdminDashboardSummaryModel(
            TotalUsers: 10,
            ActiveUsers: 8,
            PremiumUsers: 2,
            DeletedUsers: 1,
            PendingReportsCount: 3,
            RecentUsers: [CreateUser(userId, createdOnUtc)]);

        var response = model.ToHttpResponse();

        Assert.Equal(10, response.TotalUsers);
        Assert.Equal(8, response.ActiveUsers);
        Assert.Equal(2, response.PremiumUsers);
        Assert.Equal(1, response.DeletedUsers);
        Assert.Equal(3, response.PendingReportsCount);
        var user = Assert.Single(response.RecentUsers);
        Assert.Equal(userId, user.Id);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal(["Admin"], user.Roles);
        Assert.Equal(createdOnUtc, user.CreatedOnUtc);
    }

    [Fact]
    public void AdminAiUsageSummaryModel_ToHttpResponse_MapsNestedBreakdowns() {
        var userId = Guid.NewGuid();
        var model = new AdminAiUsageSummaryModel(
            TotalTokens: 300,
            InputTokens: 100,
            OutputTokens: 200,
            ByDay: [new AdminAiUsageDailyModel(new DateOnly(2026, 4, 6), 30, 10, 20)],
            ByOperation: [new AdminAiUsageBreakdownModel("meal-analysis", 90, 30, 60)],
            ByModel: [new AdminAiUsageBreakdownModel("gpt-test", 120, 40, 80)],
            ByUser: [new AdminAiUsageUserModel(userId, "user@example.com", 150, 50, 100)]);

        var response = model.ToHttpResponse();

        Assert.Equal(300, response.TotalTokens);
        Assert.Equal(100, response.InputTokens);
        Assert.Equal(200, response.OutputTokens);
        Assert.Equal(new DateOnly(2026, 4, 6), Assert.Single(response.ByDay).Date);
        Assert.Equal("meal-analysis", Assert.Single(response.ByOperation).Key);
        Assert.Equal("gpt-test", Assert.Single(response.ByModel).Key);
        var user = Assert.Single(response.ByUser);
        Assert.Equal(userId, user.Id);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal(150, user.TotalTokens);
    }

    private static AdminUserModel CreateUser(Guid id, DateTime createdOnUtc) {
        return new AdminUserModel(
            Id: id,
            Email: "user@example.com",
            HasPassword: true,
            Username: "user",
            FirstName: "Alex",
            LastName: "Tester",
            BirthDate: null,
            Gender: null,
            Weight: null,
            DesiredWeight: null,
            DesiredWaist: null,
            Height: null,
            ActivityLevel: "moderate",
            DailyCalorieTarget: null,
            ProteinTarget: null,
            FatTarget: null,
            CarbTarget: null,
            FiberTarget: null,
            StepGoal: null,
            WaterGoal: null,
            HydrationGoal: null,
            CalorieCyclingEnabled: false,
            MondayCalories: null,
            TuesdayCalories: null,
            WednesdayCalories: null,
            ThursdayCalories: null,
            FridayCalories: null,
            SaturdayCalories: null,
            SundayCalories: null,
            ProfileImage: null,
            ProfileImageAssetId: null,
            DashboardLayoutJson: null,
            Language: "en",
            Theme: "light",
            UiStyle: null,
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true,
            FastingCheckInReminderHours: 12,
            FastingCheckInFollowUpReminderHours: 2,
            TelegramUserId: null,
            IsActive: true,
            IsEmailConfirmed: true,
            CreatedOnUtc: createdOnUtc,
            DeletedAt: null,
            LastLoginAtUtc: null,
            Roles: ["Admin"],
            AiInputTokenLimit: 1000,
            AiOutputTokenLimit: 2000,
            AiConsentAcceptedAt: null);
    }
}
